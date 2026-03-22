using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Saola.Core;
using Saola.Core.Services;

namespace Saola.Plugin.Results
{
    /// <summary>
    /// Queries an ETABS database table and surfaces the result in three formats:
    ///   [0] Rows     DataTree — one branch per row, items = field values in header order
    ///   [1] Headers  List     — column names, parallel to items within each Rows branch
    ///   [2] JSON     string   — full table as JSON array, ready to write to file / Excel
    ///   [3] ETABSModel        — passthrough
    ///
    /// LOAD SELECTION:
    ///   Disconnect Load Cases / Load Combos entirely for geometry tables
    ///   (Story Definitions, Pier Section Properties) that have no load dependency.
    ///   Connect but leave blank, or pass "*", to select ALL.
    ///   Pass comma-separated names to select specific items.
    /// </summary>
    public class ExtractTableComponent : GH_Component
    {
        public ExtractTableComponent()
            : base(
                "Extract Table",
                "ExtTable",
                "Query an ETABS database table and return its rows, column headers, and JSON.",
                "Saola",
                "Results")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter(
                "Table Key", "K",
                "ETABS database table key.\n" +
                "Examples: \"Story Definitions\", \"Base Reactions\", \"Story Forces\",\n" +
                "\"Joint Drifts\", \"Pier Forces\", \"Modal Participating Mass Ratios\".",
                GH_ParamAccess.item);                        // index 0

            pManager.AddTextParameter(
                "Load Cases", "LC",
                "Load case names to include.\n" +
                "Blank or \"*\" → select ALL cases.\n" +
                "Comma-separated names → select exactly those.\n" +
                "Disconnect entirely for geometry tables with no load dependency.",
                GH_ParamAccess.list);                        // index 1

            pManager.AddTextParameter(
                "Load Combos", "LX",
                "Load combination names to include.\n" +
                "Blank or \"*\" → select ALL combos.\n" +
                "Comma-separated names → select exactly those.\n" +
                "Disconnect entirely for geometry tables with no load dependency.",
                GH_ParamAccess.list);                        // index 2

            pManager.AddTextParameter(
                "Groups", "G",
                "ETABS group names to scope the query.\n" +
                "Multiple groups are fetched separately and merged (duplicates removed).\n" +
                "Disconnect to query the whole model.",
                GH_ParamAccess.list);                        // index 3

            pManager.AddParameter(
                new ETABSModelParameter(),
                "ETABSModel", "M",
                "Live ETABS connection",
                GH_ParamAccess.item);                        // index 4

            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter(
                "Rows", "R",
                "Table rows as a DataTree.\n" +
                "Branch {i} = row i. Items within the branch = field values in Headers order.\n" +
                "Graft or index into a branch to pipe individual values downstream.",
                GH_ParamAccess.tree);                        // index 0

            pManager.AddTextParameter(
                "Headers", "H",
                "Column names in the order they appear within each Rows branch.\n" +
                "Item index i in any Rows branch corresponds to Headers[i].",
                GH_ParamAccess.list);                        // index 1

            pManager.AddTextParameter(
                "JSON", "J",
                "Full table as a JSON array of objects (one object per row, keys = field names).\n" +
                "Wire to a Write File component or import to Excel via Power Query.",
                GH_ParamAccess.item);                        // index 2

            pManager.AddParameter(
                new ETABSModelParameter(),
                "ETABSModel", "M",
                "Passthrough ETABS connection",
                GH_ParamAccess.item);                        // index 3
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // ── 1. Read inputs ────────────────────────────────────────────────

            var tableKey = string.Empty;
            if (!DA.GetData(0, ref tableKey) || string.IsNullOrWhiteSpace(tableKey))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Table Key is required.");
                return;
            }

            // Load cases — null when param is disconnected (geometry table pattern)
            var rawCases = new List<string>();
            var loadCases = DA.GetDataList(1, rawCases)
                ? ResolveLoadFilter(rawCases)
                : null;

            // Load combos — null when param is disconnected
            var rawCombos = new List<string>();
            var loadCombos = DA.GetDataList(2, rawCombos)
                ? ResolveLoadFilter(rawCombos)
                : null;

            // Groups — null when disconnected means whole model
            var rawGroups = new List<string>();
            DA.GetDataList(3, rawGroups);
            var groups = rawGroups.Count > 0
                ? rawGroups
                    .SelectMany(g => g.Split(',',
                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    .Where(g => !string.IsNullOrWhiteSpace(g))
                    .ToArray()
                : null;

            var modelGoo = new GH_ETABSModel();
            if (!DA.GetData(4, ref modelGoo) || modelGoo?.Value is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    "No ETABS connection. Connect an Initialize component.");
                return;
            }

            // ── 2. Query ──────────────────────────────────────────────────────

            var result = EtabsTableQueryService.RunQuery(
                modelGoo.Value,
                tableKey,
                loadCases,
                loadCombos,
                groups);

            if (!result.IsSuccess)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    $"Query failed for '{tableKey}': {result.ErrorMessage}");
                return;
            }

            if (result.RowCount == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    $"'{tableKey}' returned 0 rows. " +
                    "Check that load cases / combos are selected and the model is analysed.");
            }
            else
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                    $"'{tableKey}': {result.RowCount} row(s), {result.FieldKeys.Count} column(s).");
            }

            // ── 3. Build outputs ──────────────────────────────────────────────

            // Rows tree: {rowIndex} → [value0, value1, ...]  (parallel to Headers)
            var tree = new GH_Structure<GH_String>();
            for (int i = 0; i < result.Rows.Count; i++)
            {
                var path = new GH_Path(i);
                foreach (var key in result.FieldKeys)
                {
                    var value = result.Rows[i].TryGetValue(key, out var v) ? v : string.Empty;
                    tree.Append(new GH_String(value), path);
                }
            }

            var json = JsonSerializer.Serialize(
                result.Rows,
                new JsonSerializerOptions { WriteIndented = true });

            // ── 4. Write outputs ──────────────────────────────────────────────

            DA.SetDataTree(0, tree);
            DA.SetDataList(1, result.FieldKeys.Select(k => new GH_String(k)));
            DA.SetData(2, json);
            DA.SetData(3, modelGoo);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Converts raw GH string inputs to the load filter convention:
        ///   empty / ["*"] → ["*"]  (wildcard — select ALL)
        ///   ["X","Y"]     → ["X","Y"]  (exact names)
        /// Handles comma-separated values typed into a single panel.
        /// </summary>
        private static string[] ResolveLoadFilter(List<string> raw)
        {
            var items = raw
                .SelectMany(s => s.Split(',',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();

            return items.Length == 0 || (items.Length == 1 && items[0] == "*")
                ? new[] { EtabsTableQueryService.Wildcard }
                : items;
        }

        protected override System.Drawing.Bitmap Icon => null;

        public override Guid ComponentGuid =>
            new Guid("A7B3C4D5-E6F7-8901-ABCD-EF1234567890");
    }
}
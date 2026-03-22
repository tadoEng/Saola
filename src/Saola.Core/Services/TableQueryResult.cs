using EtabSharp.Core;

namespace Saola.Core.Services;

/// <summary>
/// Result of a table query issued from within Grasshopper.
/// </summary>
public sealed class TableQueryResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public string TableKey { get; init; } = string.Empty;

    /// <summary>Column names in the order returned by ETABS.</summary>
    public List<string> FieldKeys { get; init; } = new();

    /// <summary>Rows after empty-row filtering. Each dict maps fieldKey → value.</summary>
    public List<Dictionary<string, string>> Rows { get; init; } = new();

    public int RowCount => Rows.Count;

    public static TableQueryResult Fail(string tableKey, string error) => new()
    {
        IsSuccess = false,
        TableKey = tableKey,
        ErrorMessage = error,
    };
}

/// <summary>
/// Queries ETABS database tables from within a Grasshopper session (Mode A only).
///
/// LOAD SELECTION CONTRACT — same as the CLI layer:
///   null      → select NOTHING for that category.
///              Use for geometry tables (Story Definitions, Pier Section Properties).
///   ["*"]     → select ALL items of that category found in the model.
///   ["X","Y"] → select exactly those named items.
///
/// The service always resets ETABS display selection back to all-selected after
/// each query so successive GH solves start from a known clean state.
/// </summary>
public static class EtabsTableQueryService
{
    /// <summary>Wildcard sentinel — pass as the sole element to select ALL.</summary>
    public const string Wildcard = "*";

    // ── Public entry point ────────────────────────────────────────────────────

    /// <summary>
    /// Queries a single ETABS database table with optional load and group filtering.
    /// </summary>
    /// <param name="app">Live ETABSApplication from ETABSSession.GetOrConnect().</param>
    /// <param name="tableKey">ETABS database table key.</param>
    /// <param name="loadCases">
    /// null = select nothing · ["*"] = all · ["X","Y"] = exact names.
    /// </param>
    /// <param name="loadCombos">
    /// null = select nothing · ["*"] = all · ["X","Y"] = exact names.
    /// </param>
    /// <param name="groups">
    /// ETABS group names to scope the fetch. Multiple groups are fetched
    /// separately and merged (duplicates removed). null = whole model.
    /// </param>
    /// <param name="fieldKeys">Specific columns to retrieve. null = all columns.</param>
    /// <param name="discardEmptyRows">
    /// When true (default), rows where every value is null or empty are dropped.
    /// </param>
    public static TableQueryResult RunQuery(
        ETABSApplication app,
        string tableKey,
        string[]? loadCases = null,
        string[]? loadCombos = null,
        string[]? groups = null,
        string[]? fieldKeys = null,
        bool discardEmptyRows = true)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (string.IsNullOrWhiteSpace(tableKey))
            return TableQueryResult.Fail(string.Empty, "tableKey cannot be null or empty");

        try
        {
            ApplyLoadSelection(app, loadCases, loadCombos);

            // Fetch once per group, or once with no group scope
            var scopes = groups is { Length: > 0 }
                ? groups.Cast<string?>().ToArray()
                : new string?[] { null };

            var allRows = new List<Dictionary<string, string>>();
            var resolvedFieldKeys = new List<string>();
            var firstSuccessfulFetch = true;

            foreach (var group in scopes)
            {
                var raw = app.Model.DatabaseTables
                    .GetTableForDisplayArray(tableKey, fieldKeys, group ?? string.Empty);

                if (!raw.IsSuccess)
                    continue;

                if (firstSuccessfulFetch)
                {
                    resolvedFieldKeys = raw.FieldKeysIncluded;
                    firstSuccessfulFetch = false;
                }

                if (raw.NumberOfRecords > 0)
                    allRows.AddRange(raw.GetStructuredData());
            }

            // De-duplicate rows that appear in multiple overlapping groups
            var rows = Deduplicate(allRows, resolvedFieldKeys);

            if (discardEmptyRows)
                rows = rows
                    .Where(r => r.Values.Any(v => !string.IsNullOrEmpty(v)))
                    .ToList();

            return new TableQueryResult
            {
                IsSuccess = true,
                TableKey = tableKey,
                FieldKeys = resolvedFieldKeys,
                Rows = rows,
            };
        }
        catch (Exception ex)
        {
            return TableQueryResult.Fail(tableKey, ex.Message);
        }
        finally
        {
            // Always reset so the next GH solve starts from a known clean state
            ResetSelection(app);
        }
    }

    // ── Load selection ────────────────────────────────────────────────────────

    private static void ApplyLoadSelection(
        ETABSApplication app,
        string[]? loadCases,
        string[]? loadCombos)
    {
        ApplyFilter(
            filter: loadCases,
            getAll: () => app.Model.LoadCases.GetNameList() ?? Array.Empty<string>(),
            setSelected: names => app.Model.DatabaseTables.SetLoadCasesSelectedForDisplay(names));

        ApplyFilter(
            filter: loadCombos,
            getAll: () => app.Model.LoadCombinations.GetNameList() ?? Array.Empty<string>(),
            setSelected: names => app.Model.DatabaseTables.SetLoadCombinationsSelectedForDisplay(names));
    }

    private static void ApplyFilter(
        string[]? filter,
        Func<string[]> getAll,
        Action<string[]> setSelected)
    {
        if (filter is null)
            return;

        var names = IsWildcard(filter) ? SafeGetAll(getAll) : filter;
        if (names.Length > 0)
            setSelected(names);
    }

    private static void ResetSelection(ETABSApplication app)
    {
        var allCases = SafeGetAll(() => app.Model.LoadCases.GetNameList() ?? Array.Empty<string>());
        if (allCases.Length > 0)
            app.Model.DatabaseTables.SetLoadCasesSelectedForDisplay(allCases);

        var allCombos = SafeGetAll(() => app.Model.LoadCombinations.GetNameList() ?? Array.Empty<string>());
        if (allCombos.Length > 0)
            app.Model.DatabaseTables.SetLoadCombinationsSelectedForDisplay(allCombos);
    }

    private static string[] SafeGetAll(Func<string[]> getAll)
    {
        try { return getAll(); }
        catch { return Array.Empty<string>(); }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool IsWildcard(string[] filter) =>
        filter.Length == 1 && filter[0] == Wildcard;

    private static List<Dictionary<string, string>> Deduplicate(
        List<Dictionary<string, string>> rows,
        List<string> fieldKeys)
    {
        if (rows.Count == 0) return rows;

        var seen = new HashSet<string>();
        var result = new List<Dictionary<string, string>>(rows.Count);

        foreach (var row in rows)
        {
            var key = string.Join('\x1F', fieldKeys.Select(f =>
                row.TryGetValue(f, out var v) ? v : string.Empty));

            if (seen.Add(key))
                result.Add(row);
        }

        return result;
    }
}
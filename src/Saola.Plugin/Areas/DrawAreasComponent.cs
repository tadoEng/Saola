using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Saola.Core;
using Saola.Core.Services;

namespace MyNamespace
{
    public class DrawAreasComponent : GH_Component
    {
        public DrawAreasComponent()
          : base(
            "Draw Areas",
            "Areas",
            "Draw area elements (walls or slabs) in ETABS from closed vertex lists. " +
            "Use a tree input — each branch defines one area.",
            "Saola",
            "Areas")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddPointParameter(
                "Vertices", "V",
                "Closed vertex list per area. Use a tree — each branch = one area. " +
                "Last point should repeat first (standard closed polygon).",
                GH_ParamAccess.tree);          // index 0

            pManager.AddTextParameter(
                "Sections", "S",
                "Section property name applied to all areas (must exist in the model)",
                GH_ParamAccess.item);          // index 1

            pManager.AddTextParameter(
                "Area Names", "N",
                "Optional user-defined names. Leave empty to let ETABS auto-assign.",
                GH_ParamAccess.list);          // index 2

            pManager.AddParameter(
                new ETABSModelParameter(),
                "ETABSModel", "M",
                "Live ETABS connection",
                GH_ParamAccess.item);          // index 3

            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter(
                "Area Names", "N",
                "Names of the created area elements as assigned by ETABS",
                GH_ParamAccess.list);          // index 0

            pManager.AddParameter(
                new ETABSModelParameter(),
                "ETABSModel", "M",
                "Passthrough ETABS connection",
                GH_ParamAccess.item);          // index 1
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var section = string.Empty;
            var userNames = new List<string>();
            var modelGoo = new GH_ETABSModel();

            if (!DA.GetDataTree(0, out GH_Structure<GH_Point> pointTree) || pointTree.IsEmpty)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No vertices provided.");
                return;
            }

            if (!DA.GetData(1, ref section) || string.IsNullOrWhiteSpace(section))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Section name is required.");
                return;
            }

            DA.GetDataList(2, userNames);

            if (!DA.GetData(3, ref modelGoo) || modelGoo?.Value is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    "No ETABS connection. Connect an Initialize component.");
                return;
            }

            var branches = pointTree.Branches;

            if (userNames.Count > 0 && userNames.Count != branches.Count)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    $"Area Names count ({userNames.Count}) must match branch count ({branches.Count}), or leave empty.");
                return;
            }

            var areaBranches = branches
                .Select(b => (IList<Point3d>)b
                    .Where(p => p != null && p.IsValid)
                    .Select(p => p.Value)
                    .ToList())
                .ToList();

            var result = AreaService.AddAreas(
                modelGoo.Value.Model,
                areaBranches,
                section,
                userNames.Count > 0 ? userNames : null);

            foreach (var (index, message) in result.Errors)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, message);

            DA.SetDataList(0, result.Names);
            DA.SetData(1, modelGoo);
        }

        protected override System.Drawing.Bitmap Icon => null;

        public override Guid ComponentGuid =>
            new Guid("E3E8F089-843B-4DAB-B6E6-EC2B98B8116C");
    }
}
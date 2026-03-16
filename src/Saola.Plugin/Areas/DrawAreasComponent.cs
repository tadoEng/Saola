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
        /// <summary>
        /// Initializes a new instance of the DrawAreasComponent class.
        /// </summary>
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

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter(
                "Vertices", "V",
                "Closed vertex list per area. Use a tree — each branch = one area. " +
                "Last point should repeat first (standard closed polygon).",
                GH_ParamAccess.tree);

            pManager.AddTextParameter(
                "Sections", "S",
                "Section property name applied to all areas (must exist in the model)",
                GH_ParamAccess.item);

            pManager.AddTextParameter(
                "Area Names", "N",
                "Optional user-defined names. Leave empty to let ETABS auto-assign.",
                GH_ParamAccess.list);
            
            pManager.AddParameter(
                new ETABSModelParameter(),
                "ETABSModel", "M",
                "Live ETABS connection",
                GH_ParamAccess.item);

            pManager[2].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter(
            "Area Names", "N",
            "Names of the created area elements as assigned by ETABS",
            GH_ParamAccess.list);

            pManager.AddParameter(
                new ETABSModelParameter(),
                "ETABSModel", "M",
                "Passthrough ETABS connection",
                GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // --- Read inputs ---
            var modelGoo = new GH_ETABSModel();
            var section = string.Empty;
            var userNames = new List<string>();

            if (!DA.GetData(0, ref modelGoo) || modelGoo?.Value is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    "No ETABS connection. Connect an Initialize component.");
                return;
            }

            if (!DA.GetDataTree(1, out GH_Structure<GH_Point> pointTree) || pointTree.IsEmpty)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No vertices provided.");
                return;
            }

            if (!DA.GetData(2, ref section) || string.IsNullOrWhiteSpace(section))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Section name is required.");
                return;
            }

            DA.GetDataList(3, userNames);

            // --- Convert GH tree branches to IList<IList<Point3d>> for the service ---
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

            // --- Delegate to service ---
            var result = AreaService.AddAreas(
                modelGoo.Value.Model,
                areaBranches,
                section,
                userNames.Count > 0 ? userNames : null);

            // --- Report per-item errors as GH warnings ---
            foreach (var (index, message) in result.Errors)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, message);

            // --- Outputs ---
            DA.SetDataList(0, result.Names);
            DA.SetData(1, modelGoo);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("E3E8F089-843B-4DAB-B6E6-EC2B98B8116C"); }
        }
    }
}
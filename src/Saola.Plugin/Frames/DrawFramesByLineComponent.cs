using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Saola.Core;
using Saola.Core.Services;

namespace MyNamespace
{
    public class DrawFramesByLineComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the DrawFramesByLineComponent class.
        /// </summary>
        public DrawFramesByLineComponent()
          : base(
            "Draw Frames by Line",
            "Frames",
            "Draw frame elements in ETABS from lines and a section name",
            "Saola",
            "Frames")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {

            pManager.AddLineParameter(
                "Lines", "L",
                "Lines defining frame axes. Each line becomes one frame element.",
                GH_ParamAccess.list);

            pManager.AddTextParameter(
                "Sections", "S",
                "Section property name applied to all lines (must exist in the model)",
                GH_ParamAccess.item);

            pManager.AddTextParameter(
                "Frame Names", "N",
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
            "Frame Names", "N",
            "Names of the created frame elements as assigned by ETABS",
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
            var lines = new List<Line>();
            var section = string.Empty;
            var userNames = new List<string>();
            var modelGoo = new GH_ETABSModel();

            if (!DA.GetDataList(0, lines) || lines.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No lines provided.");
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

            if (userNames.Count > 0 && userNames.Count != lines.Count)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    $"Frame Names count ({userNames.Count}) must match Lines count ({lines.Count}), or leave empty.");
                return;
            }

            var result = FrameService.AddFramesByLines(
                modelGoo.Value.Model,
                lines,
                section,
                userNames.Count > 0 ? userNames : null);

            foreach (var (index, message) in result.Errors)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, message);

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
            get { return new Guid("21F0567E-3CC9-4105-A8BC-07C1E55881EC"); }
        }
    }
}
using Grasshopper.Kernel;
using Saola.Core;
using Saola.Core.Services;
using System;
using System.Collections.Generic;

namespace Saola.Plugin.Model
{
    public class SetStoriesComponent : GH_Component
    {
        public SetStoriesComponent()
          : base(
            "Set Stories",
            "Stories",
            "Define story levels in the ETABS model. " +
            "Must be called before drawing any geometry. " +
            "Story names go from bottom to top.",
            "Saola",
            "Model")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter(
                "Story Names", "N",
                "Story names from bottom to top (e.g. STORY1, STORY2...)",
                GH_ParamAccess.list);              // index 0

            pManager.AddNumberParameter(
                "Story Heights", "H",
                "Story heights in model units (ft for kip-ft), bottom to top",
                GH_ParamAccess.list);              // index 1

            pManager.AddNumberParameter(
                "Base Elevation", "E",
                "Elevation of the base level (default 0)",
                GH_ParamAccess.item,
                0.0);                              // index 2

            pManager.AddParameter(
                new ETABSModelParameter(),
                "ETABSModel", "M",
                "Live ETABS connection",
                GH_ParamAccess.item);              // index 3

            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(
                new ETABSModelParameter(),
                "ETABSModel", "M",
                "Passthrough ETABS connection",
                GH_ParamAccess.item);              // index 0
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var storyNames = new List<string>();
            var storyHeights = new List<double>();
            double baseElevation = 0.0;
            var modelGoo = new GH_ETABSModel();

            if (!DA.GetDataList(0, storyNames) || storyNames.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Story names are required.");
                return;
            }

            if (!DA.GetDataList(1, storyHeights) || storyHeights.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Story heights are required.");
                return;
            }

            DA.GetData(2, ref baseElevation);

            if (!DA.GetData(3, ref modelGoo) || modelGoo?.Value is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    "No ETABS connection. Connect an Initialize component.");
                return;
            }

            var result = StoryService.SetStories(
                modelGoo.Value.Model,
                storyNames,
                storyHeights,
                baseElevation);

            if (!result.IsSuccess)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, result.Message);
                return;
            }

            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, result.Message);
            DA.SetData(0, modelGoo);
        }

        protected override System.Drawing.Bitmap Icon => null;

        public override Guid ComponentGuid =>
            new Guid("DE6BA43B-7C83-4F6B-9A2B-C7F10BF7A4ED");
    }
}
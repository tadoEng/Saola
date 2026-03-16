using System;

using Grasshopper.Kernel;
using Saola.Core;

namespace Saola.Plugin.Initialize
{
    public class InitializeComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the InitializeComponent class.
        /// </summary>
        public InitializeComponent()
          : base(
            "Initialize",
            "Init",
            "Connect to a running ETABS instance",
            "Saola",
            "Connection")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(
            new ETABSModelParameter(),
            "ETABSModel", "M",
            "Live ETABS connection",
            GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var app = ETABSSession.GetOrConnect();

            if (app is null)
            {
                AddRuntimeMessage(
                    GH_RuntimeMessageLevel.Error,
                    "No running ETABS instance found. Open ETABS first, then re-compute.");
                return;
            }

            AddRuntimeMessage(
                GH_RuntimeMessageLevel.Remark,
                $"Connected to ETABS {app.FullVersion}");

            DA.SetData(0, new GH_ETABSModel(app));
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
            get { return new Guid("D598731A-05EF-47AE-A452-63B93FB7C800"); }
        }
    }
}
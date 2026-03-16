using System;
using Grasshopper.Kernel;
using Saola.Core;

namespace Saola.Plugin.Initialize
{
    public class InitializeComponent : GH_Component
    {
        public InitializeComponent()
          : base(
            "Initialize",
            "Init",
            "Connect to a running ETABS instance",
            "Saola",
            "Connection")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter(
                "Connect", "C",
                "Set to True to connect to ETABS",
                GH_ParamAccess.item,
                false);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(
                new ETABSModelParameter(),
                "ETABSModel", "M",
                "Live ETABS connection",
                GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool connect = false;
            DA.GetData(0, ref connect);

            if (!connect)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    "Set Connect to True to initialize ETABS connection.");
                return;
            }

            var app = ETABSSession.GetOrConnect();

            if (app is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    "No running ETABS instance found. Open ETABS first.");
                return;
            }

            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                $"Connected to ETABS {app.FullVersion}");

            DA.SetData(0, new GH_ETABSModel(app));
        }

        protected override System.Drawing.Bitmap Icon => null;

        public override Guid ComponentGuid =>
            new Guid("D598731A-05EF-47AE-A452-63B93FB7C800");
    }
}
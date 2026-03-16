using Grasshopper.Kernel;

namespace Saola.Core;

/// <summary>
/// Custom Grasshopper parameter for ETABSModel wires.
/// Gives a distinct wire type and color on the GH canvas.
/// </summary>
public class ETABSModelParameter : GH_Param<GH_ETABSModel>
{
    public ETABSModelParameter()
        : base(
            new GH_InstanceDescription(
                "ETABS Model",
                "Model",
                "A live ETABS model connection",
                "Saola",
                "Parameters"))
    {
    }

    public override Guid ComponentGuid =>
        new("C1D2E3F4-A5B6-7890-CDEF-123456789ABC");

    
}

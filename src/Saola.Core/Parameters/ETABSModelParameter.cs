using Grasshopper.Kernel;

namespace Saola.Core;



/// <summary>
/// Custom Grasshopper parameter for ETABSModel wires.
///
/// Gives a distinct wire type on the GH canvas — engineers can visually
/// distinguish the ETABS connection wire from geometry or text wires.
/// Type-checking happens at wire-time, not at solve-time.
///
/// TypeName ("ETABSModel") comes from GH_ETABSModel.TypeName — GH_Param
/// resolves it by calling TypeName on the first instance of T it finds.
/// </summary>
public class ETABSModelParameter : GH_Param<GH_ETABSModel>
{
    public ETABSModelParameter()
        : base(
            "ETABSModel",      // name
            "M",               // nickname
            "A live ETABS model connection",  // description
            "Saola",           // category
            "Parameters",      // subcategory
            GH_ParamAccess.item)
    {
    }

    public override Guid ComponentGuid =>
        new("C1D2E3F4-A5B6-7890-CDEF-123456789ABC");

}

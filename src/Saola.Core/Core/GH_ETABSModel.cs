using EtabSharp;
using EtabSharp.Core;
using Grasshopper.Kernel.Types;

namespace Saola.Core;

/// <summary>
/// Grasshopper Goo wrapper for a live ETABSApplication connection.
/// Required for passing the connection through GH wires with a distinct
/// wire type and label ("ETABSModel") on the canvas.
/// </summary>
public class GH_ETABSModel : GH_Goo<ETABSApplication>
{
    public GH_ETABSModel() { }

    public GH_ETABSModel(ETABSApplication app)
    {
        Value = app;
    }

    public override bool IsValid => Value is not null;

    public override string IsValidWhyNot =>
        Value is null ? "No ETABS connection" : string.Empty;

    public override string TypeName => "ETABSModel";

    public override string TypeDescription =>
        "A live connection to a running ETABS instance";

    public override IGH_Goo Duplicate() => new GH_ETABSModel(Value!);

    public override string ToString() =>
        Value is null
            ? "No ETABS connection"
            : $"ETABS {Value.FullVersion}";

    public override bool CastFrom(object source)
    {
        if (source is ETABSApplication app)
        {
            Value = app;
            return true;
        }
        return false;
    }

    public override bool CastTo<T>(ref T target)
    {
        if (typeof(T) == typeof(ETABSApplication))
        {
            target = (T)(object)Value!;
            return true;
        }
        return false;
    }
}

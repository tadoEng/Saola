using EtabSharp.Core;

namespace Saola.Core;

/// <summary>
/// Document-scoped singleton for the ETABS COM connection.
/// Prevents multiple COM connections from Grasshopper recompute cycles.
/// </summary>
public static class ETABSSession
{
    private static ETABSApplication? _instance;

    public static ETABSApplication? GetOrConnect()
    {
        if (_instance == null || !IsAlive(_instance))
        {
            _instance = ETABSWrapper.Connect();
        }

        return _instance;
    }

    public static void Reset()
    {
        _instance = null;
    }

    private static bool IsAlive(ETABSApplication app)
    {
        try
        {
            _ = app.FullVersion;
            return true;
        }
        catch
        {
            return false;
        }
    }
}

using EtabSharp.Core;
using Microsoft.Extensions.Logging.Abstractions;

namespace Saola.Core;

/// <summary>
/// Document-scoped singleton for the ETABS COM connection.
///
/// Why this exists:
/// Grasshopper calls SolveInstance() on every recompute — which can fire dozens
/// of times per session. Without a singleton, each recompute would open a new COM
/// connection, which is slow and can destabilize ETABS. ETABSSession holds exactly
/// one live ETABSApplication reference and reuses it until it detects the connection
/// has dropped (e.g. ETABS was closed), at which point it reconnects automatically.
/// </summary>
public static class ETABSSession
{
    private static ETABSApplication? _instance;

    /// <summary>
    /// Returns the live ETABS connection, reconnecting if needed.
    /// Returns null if no ETABS process is running.
    /// </summary>
    public static ETABSApplication? GetOrConnect()
    {
        if (_instance == null || !IsAlive(_instance))
            // TODO: bypass for now, but we should probably add some user-facing logging in the future
            _instance = ETABSWrapper.Connect(NullLogger<ETABSApplication>.Instance); 

        return _instance;
    }

    /// <summary>
    /// Clears the cached connection. Call this if you want to force a fresh
    /// connect on the next recompute (e.g. after ETABS restarts).
    /// </summary>
    public static void Reset() => _instance = null;

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
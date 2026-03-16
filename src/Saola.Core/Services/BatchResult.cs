namespace Saola.Core.Services;

/// <summary>
/// Result of a batch operation (e.g. creating multiple frames or areas).
/// Always contains the full list of names — empty string for items that failed.
/// Errors are collected per-index so the component layer can report them
/// as GH runtime messages without aborting the whole batch.
/// </summary>
public sealed class BatchResult
{
    /// <summary>
    /// Created element names in input order.
    /// Empty string at index i means item i failed — check Errors for details.
    /// </summary>
    public List<string> Names { get; } = new();

    /// <summary>
    /// Per-item errors keyed by input index.
    /// Empty if all items succeeded.
    /// </summary>
    public Dictionary<int, string> Errors { get; } = new();

    public bool HasErrors => Errors.Count > 0;

    public bool AllFailed => Names.All(string.IsNullOrEmpty);
}

using EtabSharp.Core;
using Rhino.Geometry;
using Saola.Core.Services.Models;

namespace Saola.Core.Services;

/// <summary>
/// Pure ETABS frame creation logic — no Grasshopper dependency.
/// Testable in isolation, reusable from CLI tools or Tauri apps.
/// </summary>
public static class FrameService
{
    /// <summary>
    /// Creates frame elements from a list of lines using a single section.
    /// Processes all items and collects errors rather than aborting on first failure.
    /// </summary>
    /// <param name="model">Live ETABS model from ETABSApplication.Model</param>
    /// <param name="lines">Lines defining frame axes</param>
    /// <param name="section">Section property name — must exist in the model</param>
    /// <param name="userNames">
    /// Optional user-defined names. Must match lines count if provided.
    /// Pass null or empty to let ETABS auto-assign names.
    /// </param>
    /// <returns>
    /// BatchResult with created names in input order.
    /// Empty string at index i = item i failed, check BatchResult.Errors[i].
    /// </returns>
    public static BatchResult AddFramesByLines(
        ETABSModel model,
        IList<Line> lines,
        string section,
        IList<string>? userNames = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(lines);

        if (string.IsNullOrWhiteSpace(section))
            throw new ArgumentException("Section name cannot be null or empty.", nameof(section));

        if (userNames != null && userNames.Count > 0 && userNames.Count != lines.Count)
            throw new ArgumentException(
                $"userNames count ({userNames.Count}) must match lines count ({lines.Count}).",
                nameof(userNames));

        var result = new BatchResult();

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            if (!line.IsValid)
            {
                result.Names.Add(string.Empty);
                result.Errors[i] = $"Line [{i}] is not valid — skipped.";
                continue;
            }

            if (line.Length < 1e-6)
            {
                result.Names.Add(string.Empty);
                result.Errors[i] = $"Line [{i}] has zero length — skipped.";
                continue;
            }

            var userName = (userNames != null && userNames.Count > 0)
                ? userNames[i]
                : string.Empty;

            try
            {
                var name = model.Frames.AddFrameByCoordinates(
                    line.FromX, line.FromY, line.FromZ,
                    line.ToX,   line.ToY,   line.ToZ,
                    section, userName);

                result.Names.Add(name);
            }
            catch (Exception ex)
            {
                result.Names.Add(string.Empty);
                result.Errors[i] = $"Frame [{i}]: {ex.Message}";
            }
        }

        return result;
    }
}

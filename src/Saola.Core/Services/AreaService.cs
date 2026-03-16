using EtabSharp.Core;
using EtabSharp.Elements.AreaObj.Models;
using Rhino.Geometry;

namespace Saola.Core.Services;

/// <summary>
/// Pure ETABS area creation logic — no Grasshopper dependency.
/// Testable in isolation, reusable from CLI tools or Tauri apps.
/// </summary>
public static class AreaService
{
    /// <summary>
    /// Creates area elements from a list of closed vertex lists using a single section.
    /// Works for both walls (vertical geometry) and slabs (horizontal geometry) —
    /// the section property determines element type, not this service.
    ///
    /// Processes all items and collects errors rather than aborting on first failure.
    /// </summary>
    /// <param name="model">Live ETABS model from ETABSApplication.Model</param>
    /// <param name="areaBranches">
    /// One list of Point3d per area. Each list should be a closed polygon
    /// (last point ≈ first point). The closing point is stripped automatically.
    /// </param>
    /// <param name="section">Section property name — must exist in the model</param>
    /// <param name="userNames">
    /// Optional user-defined names. Must match areaBranches count if provided.
    /// Pass null or empty to let ETABS auto-assign names.
    /// </param>
    /// <returns>
    /// BatchResult with created names in input order.
    /// Empty string at index i = item i failed, check BatchResult.Errors[i].
    /// </returns>
    public static BatchResult AddAreas(
        ETABSModel model,
        IList<IList<Point3d>> areaBranches,
        string section,
        IList<string>? userNames = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(areaBranches);

        if (string.IsNullOrWhiteSpace(section))
            throw new ArgumentException("Section name cannot be null or empty.", nameof(section));

        if (userNames != null && userNames.Count > 0 && userNames.Count != areaBranches.Count)
            throw new ArgumentException(
                $"userNames count ({userNames.Count}) must match areaBranches count ({areaBranches.Count}).",
                nameof(userNames));

        var result = new BatchResult();

        for (int i = 0; i < areaBranches.Count; i++)
        {
            var pts = areaBranches[i]?.ToList();

            if (pts == null || pts.Count == 0)
            {
                result.Names.Add(string.Empty);
                result.Errors[i] = $"Area [{i}] has no points — skipped.";
                continue;
            }

            if (pts.Count < 4)
            {
                result.Names.Add(string.Empty);
                result.Errors[i] =
                    $"Area [{i}] has {pts.Count} point(s). " +
                    "Need at least 4 (closed list with 3 unique corners).";
                continue;
            }

            // Strip closing point if list is closed (last ≈ first)
            if (pts[0].DistanceTo(pts[^1]) < 0.001)
                pts.RemoveAt(pts.Count - 1);

            if (pts.Count < 3)
            {
                result.Names.Add(string.Empty);
                result.Errors[i] =
                    $"Area [{i}] has fewer than 3 unique points after stripping the closing point.";
                continue;
            }

            var coords = pts
                .Select(p => new AreaCoordinate(p.X, p.Y, p.Z))
                .ToArray();

            var userName = (userNames != null && userNames.Count > 0)
                ? userNames[i]
                : string.Empty;

            try
            {
                var name = model.Areas.AddAreaByCoordinates(coords, section, userName);
                result.Names.Add(name);
            }
            catch (Exception ex)
            {
                result.Names.Add(string.Empty);
                result.Errors[i] = $"Area [{i}]: {ex.Message}";
            }
        }

        return result;
    }
}

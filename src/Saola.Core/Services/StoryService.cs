using EtabSharp.Core;
using EtabSharp.Elements.Story.Models;
using Saola.Core.Services.Models;

namespace Saola.Core.Services;

/// <summary>
/// Pure ETABS story definition logic — no Grasshopper dependency.
/// Testable in isolation, reusable from CLI tools or Tauri apps.
/// </summary>
public static class StoryService
{
    /// <summary>
    /// Defines story levels in the ETABS model.
    /// Must be called before drawing any geometry — stories are a prerequisite
    /// for all element placement in ETABS.
    /// </summary>
    /// <param name="model">Live ETABS model from ETABSApplication.Model</param>
    /// <param name="storyNames">
    /// Story names from bottom to top (e.g. "STORY1", "STORY2").
    /// Count must match storyHeights.
    /// </param>
    /// <param name="storyHeights">
    /// Story heights in model units (ft for kip-ft), bottom to top.
    /// Count must match storyNames.
    /// </param>
    /// <param name="baseElevation">Elevation of the base level. Defaults to 0.</param>
    /// <returns>
    /// ServiceResult indicating success or a descriptive error message.
    /// </returns>
    public static ServiceResult SetStories(
        ETABSModel model,
        IList<string> storyNames,
        IList<double> storyHeights,
        double baseElevation = 0)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(storyNames);
        ArgumentNullException.ThrowIfNull(storyHeights);

        if (storyNames.Count == 0)
            return ServiceResult.Fail("Story names cannot be empty.");

        if (storyHeights.Count == 0)
            return ServiceResult.Fail("Story heights cannot be empty.");

        if (storyNames.Count != storyHeights.Count)
            return ServiceResult.Fail(
                $"Story names count ({storyNames.Count}) must match heights count ({storyHeights.Count}).");

        try
        {
            var storyData = StoryData.CreateDefault(
                baseElevation,
                storyNames.ToArray(),
                storyHeights.ToArray());

            model.Story.SetStories(storyData);

            return ServiceResult.Ok($"{storyNames.Count} stories set successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResult.Fail($"Failed to set stories: {ex.Message}");
        }
    }
}
using EtabSharp.Core;
using EtabSharp.Elements.Story.Models;

namespace Saola.Core.Services;

public static class StoryService
{
    public static void SetStories(
        ETABSModel model,
        IList<string> storyNames,
        IList<double> storyHeights,
        double baseElevation = 0)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (storyNames == null || storyNames.Count == 0)
            throw new ArgumentException("Story names cannot be empty.", nameof(storyNames));

        if (storyHeights == null || storyHeights.Count == 0)
            throw new ArgumentException("Story heights cannot be empty.", nameof(storyHeights));

        if (storyNames.Count != storyHeights.Count)
            throw new ArgumentException(
                $"Story names count ({storyNames.Count}) must match heights count ({storyHeights.Count}).");

        var storyData = StoryData.CreateDefault(
            baseElevation,
            storyNames.ToArray(),
            storyHeights.ToArray());

        model.Story.SetStories(storyData);
    }
}
using OpenSubtitleFetcher.Domain.Models.Enums;

namespace OpenSubtitleFetcher.Domain.Models.Subtitles.Selection;

public abstract class SubtitleSelectionStrategyBase : ISubtitleSelectionStrategy
{
    public abstract SubtitleSelectionMode Mode { get; }

    public SubtitleCandidate? Select(IReadOnlyCollection<SubtitleCandidate> candidates)
    {
        var usable = candidates.Where(x => x.Files.Count == 1).ToList();
        return usable.Count == 0 ? null : Order(usable).First();
    }

    protected abstract IEnumerable<SubtitleCandidate> Order(IEnumerable<SubtitleCandidate> candidates);
}
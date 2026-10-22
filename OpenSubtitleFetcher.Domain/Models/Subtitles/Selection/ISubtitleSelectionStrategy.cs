using OpenSubtitleFetcher.Domain.Models.Enums;

namespace OpenSubtitleFetcher.Domain.Models.Subtitles.Selection;

public interface ISubtitleSelectionStrategy
{
    SubtitleSelectionMode Mode { get; }
    SubtitleCandidate? Select(IReadOnlyCollection<SubtitleCandidate> candidates);
}
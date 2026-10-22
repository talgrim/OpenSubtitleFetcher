using OpenSubtitleFetcher.Domain.Models.Enums;

namespace OpenSubtitleFetcher.Domain.Models.Subtitles.Selection;

public class BestMatchStrategy : SubtitleSelectionStrategyBase
{
    public override SubtitleSelectionMode Mode => SubtitleSelectionMode.BestMatch;

    protected override IEnumerable<SubtitleCandidate> Order(IEnumerable<SubtitleCandidate> candidates)
    {
        return candidates.OrderByDescending(x => x.MovieHashMatch)
            .ThenBy(x => x.MachineTranslated)
            .ThenByDescending(x => x.FromTrusted)
            .ThenByDescending(x => x.DownloadCount);
    }
}
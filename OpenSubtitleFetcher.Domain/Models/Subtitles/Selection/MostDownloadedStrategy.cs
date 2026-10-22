using OpenSubtitleFetcher.Domain.Models.Enums;

namespace OpenSubtitleFetcher.Domain.Models.Subtitles.Selection;

public class MostDownloadedStrategy : SubtitleSelectionStrategyBase
{
    public override SubtitleSelectionMode Mode => SubtitleSelectionMode.MostDownloaded;

    protected override IEnumerable<SubtitleCandidate> Order(IEnumerable<SubtitleCandidate> candidates)
    {
        return candidates.OrderByDescending(x => x.DownloadCount);
    }
}
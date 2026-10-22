using OpenSubtitleFetcher.Domain.Models.Enums;

namespace OpenSubtitleFetcher.Domain.Models.Subtitles.Selection;

public class TrustedFirstStrategy : SubtitleSelectionStrategyBase
{
    public override SubtitleSelectionMode Mode => SubtitleSelectionMode.TrustedFirst;

    protected override IEnumerable<SubtitleCandidate> Order(IEnumerable<SubtitleCandidate> candidates)
    {
        return candidates.OrderBy(x => x.MachineTranslated)
            .ThenByDescending(x => x.FromTrusted)
            .ThenByDescending(x => x.DownloadCount);
    }
}
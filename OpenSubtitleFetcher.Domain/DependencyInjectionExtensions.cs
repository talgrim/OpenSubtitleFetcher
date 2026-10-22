using Microsoft.Extensions.DependencyInjection;
using OpenSubtitleFetcher.Domain.Models.Subtitles.Selection;

namespace OpenSubtitleFetcher.Domain;

public static class DependencyInjectionExtensions
{
    public static void RegisterDomain(this IServiceCollection services)
    {
        services.AddSingleton<ISubtitleSelectionStrategy, MostDownloadedStrategy>();
        services.AddSingleton<ISubtitleSelectionStrategy, TrustedFirstStrategy>();
        services.AddSingleton<ISubtitleSelectionStrategy, BestMatchStrategy>();
    }
}
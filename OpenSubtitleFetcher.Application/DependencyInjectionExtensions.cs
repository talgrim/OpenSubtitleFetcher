using Microsoft.Extensions.DependencyInjection;
using OpenSubtitleFetcher.Application.UseCases;
using OpenSubtitleFetcher.Domain.Models.Subtitles.Selection;

namespace OpenSubtitleFetcher.Application;

public static class DependencyInjectionExtensions
{
    extension(IServiceCollection services)
    {
        public void RegisterApplication()
        {
            services.AddTransient<IDownloadSubtitlesUseCase, DownloadSubtitlesUseCase>();
            services.AddTransient<IGetVideoFilesUseCase, GetVideoFilesUseCase>();

            services.RegisterDomain();
        }

        private void RegisterDomain()
        {
            services.AddSingleton<ISubtitleSelectionStrategy, MostDownloadedStrategy>();
            services.AddSingleton<ISubtitleSelectionStrategy, TrustedFirstStrategy>();
            services.AddSingleton<ISubtitleSelectionStrategy, BestMatchStrategy>();
        }
    }
}
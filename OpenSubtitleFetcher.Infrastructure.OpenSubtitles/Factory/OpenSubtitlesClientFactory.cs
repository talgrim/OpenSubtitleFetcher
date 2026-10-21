using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using OpenSubtitleFetcher.Infrastructure.RestApi;
using RestEase;

namespace OpenSubtitleFetcher.Infrastructure.OpenSubtitles.Factory;

internal interface IOpenSubtitlesClientFactory
{
    IOpenSubtitlesApiSubtitles CreateRestClient();
}

internal class OpenSubtitlesClientFactory : IOpenSubtitlesClientFactory
{
    public const string HttpClientName = "OpenSubtitleFetcher";

    private readonly OpenSubtitlesSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly JsonSerializerSettings _jsonSerializerSettings;

    public OpenSubtitlesClientFactory(IOptions<OpenSubtitlesSettings> settingsOptions, IHttpClientFactory httpClientFactory)
    {
        _settings = settingsOptions.Value;
        _httpClientFactory = httpClientFactory;

        _jsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            }
        };
    }

    public IOpenSubtitlesApiSubtitles CreateRestClient()
    {
        var httpClient = _httpClientFactory.CreateClient(HttpClientName);
        httpClient.BaseAddress = new Uri($"{_settings.Url}/v{_settings.Version}");

        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("Api-Key", _settings.ApiKey);
        httpClient.DefaultRequestHeaders.Add("User-Agent", _settings.UserAgent);

        return new RestClient(httpClient)
        {
            JsonSerializerSettings = _jsonSerializerSettings
        }.For<IOpenSubtitlesApiSubtitles>();
    }
}
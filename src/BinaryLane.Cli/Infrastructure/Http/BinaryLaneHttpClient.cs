using System.Net.Http.Headers;
using BinaryLane.Cli.Infrastructure.Configuration;

namespace BinaryLane.Cli.Infrastructure.Http;

public sealed class BinaryLaneHttpClient : IDisposable
{
    private readonly HttpClient _client;
    private readonly CurlGenerator _curlGenerator;

    public CurlGenerator CurlGenerator => _curlGenerator;

    public BinaryLaneHttpClient(AppConfiguration config)
    {
        _curlGenerator = new CurlGenerator();

        var handler = new HttpClientHandler();
        if (config.ApiDevelopment)
        {
            handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
        }

        _curlGenerator.InnerHandler = handler;
        _client = new HttpClient(_curlGenerator);

        _client.BaseAddress = new Uri(config.ApiUrl.TrimEnd('/') + "/");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiToken);
        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public HttpClient Client => _client;

    public void Dispose() => _client.Dispose();
}

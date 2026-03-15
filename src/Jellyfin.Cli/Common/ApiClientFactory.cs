using Jellyfin.Cli.Api.Generated;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace Jellyfin.Cli.Common;

public sealed class ApiClientFactory
{
    public JellyfinApiClient CreateClient(string baseUrl, string? token = null, string? apiKey = null)
    {
        var authProvider = new TokenAuthenticationProvider(token, apiKey);
        var httpClient = new HttpClient(CreatePrimaryHandler());
        var adapter = new HttpClientRequestAdapter(authProvider, httpClient: httpClient)
        {
            BaseUrl = baseUrl.TrimEnd('/'),
        };
        return new JellyfinApiClient(adapter);
    }

    public HttpClient CreateHttpClient(string baseUrl, string? token = null, string? apiKey = null)
    {
        var httpClient = new HttpClient(CreatePrimaryHandler())
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
        };

        httpClient.DefaultRequestHeaders.TryAddWithoutValidation(
            "Authorization",
            TokenAuthenticationProvider.BuildAuthorizationHeader(token, apiKey));

        return httpClient;
    }

    private static HttpMessageHandler CreatePrimaryHandler()
    {
        return new DiagnosticLoggingHandler
        {
            InnerHandler = new GuidNormalizingHandler
            {
                InnerHandler = new HttpClientHandler(),
            },
        };
    }
}

internal sealed class TokenAuthenticationProvider : IAuthenticationProvider
{
    private static readonly string DeviceId = GetDeviceId();
    private readonly string? _token;
    private readonly string? _apiKey;

    public TokenAuthenticationProvider(string? token, string? apiKey)
    {
        _token = token;
        _apiKey = apiKey;
    }

    public Task AuthenticateRequestAsync(
        RequestInformation request,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default)
    {
        request.Headers.Add("Authorization", BuildAuthorizationHeader(_token, _apiKey));

        return Task.CompletedTask;
    }

    internal static string BuildAuthorizationHeader(string? token, string? apiKey)
    {
        var authValue = $"MediaBrowser Client=\"Jellyfin CLI\", Device=\"{Environment.MachineName}\", DeviceId=\"{DeviceId}\", Version=\"0.1.0\"";

        if (!string.IsNullOrEmpty(token))
        {
            authValue += $", Token=\"{token}\"";
        }
        else if (!string.IsNullOrEmpty(apiKey))
        {
            authValue += $", Token=\"{apiKey}\"";
        }

        return authValue;
    }

    private static string GetDeviceId()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var idFile = Path.Combine(appData, "jf", "device-id");

        if (File.Exists(idFile))
        {
            var stored = File.ReadAllText(idFile).Trim();
            if (!string.IsNullOrEmpty(stored))
                return stored;
        }

        var id = Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(Path.GetDirectoryName(idFile)!);
        File.WriteAllText(idFile, id);
        return id;
    }
}

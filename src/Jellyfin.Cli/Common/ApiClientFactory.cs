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
        var httpClient = new HttpClient(new GuidNormalizingHandler
        {
            InnerHandler = new HttpClientHandler(),
        });
        var adapter = new HttpClientRequestAdapter(authProvider, httpClient: httpClient)
        {
            BaseUrl = baseUrl.TrimEnd('/'),
        };
        return new JellyfinApiClient(adapter);
    }
}

internal sealed class TokenAuthenticationProvider : IAuthenticationProvider
{
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
        if (!string.IsNullOrEmpty(_token))
        {
            request.Headers.Add("Authorization", $"MediaBrowser Token=\"{_token}\"");
        }
        else if (!string.IsNullOrEmpty(_apiKey))
        {
            request.Headers.Add("Authorization", $"MediaBrowser Token=\"{_apiKey}\"");
        }

        return Task.CompletedTask;
    }
}

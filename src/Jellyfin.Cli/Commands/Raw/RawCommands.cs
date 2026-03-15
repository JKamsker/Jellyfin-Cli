using System.ComponentModel;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Raw;

// ---------------------------------------------------------------------------
// Settings shared by all raw commands
// ---------------------------------------------------------------------------

public class RawSettings : GlobalSettings
{
    [CommandArgument(0, "<PATH>")]
    [Description("API path (e.g. /System/Info)")]
    public string Path { get; set; } = string.Empty;

    [CommandOption("--query <QUERY>")]
    [Description("Query string parameters (key=value), can be repeated")]
    public string[]? Query { get; set; }

    [CommandOption("--header <HEADER>")]
    [Description("Extra headers (Key: Value), can be repeated")]
    public string[]? Headers { get; set; }

    [CommandOption("--body <BODY>")]
    [Description("Request body (JSON string)")]
    public string? Body { get; set; }

    [CommandOption("--accept <ACCEPT>")]
    [Description("Accept header value")]
    public string? Accept { get; set; }

    [CommandOption("--download <FILE>")]
    [Description("Download response to a file instead of printing")]
    public string? Download { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Path))
            return ValidationResult.Error("PATH argument is required.");

        return ValidationResult.Success();
    }
}

// ---------------------------------------------------------------------------
// Base class for raw HTTP commands
// ---------------------------------------------------------------------------

public abstract class RawCommandBase : AsyncCommand<RawSettings>
{
    private readonly ApiClientFactory _clientFactory;
    private readonly CredentialStore _credentialStore;

    protected RawCommandBase(ApiClientFactory clientFactory, CredentialStore credentialStore)
    {
        _clientFactory = clientFactory;
        _credentialStore = credentialStore;
    }

    protected abstract HttpMethod Method { get; }

    public override async Task<int> ExecuteAsync(
        CommandContext context, RawSettings settings, CancellationToken cancellationToken)
    {
        HttpDiagnosticsContext.Clear();
        var (server, token, apiKey) = ResolveCredentials(settings);

        if (string.IsNullOrEmpty(server))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] No server URL. Use --server or run 'jf auth login'.");
            return 1;
        }

        var url = BuildUrl(server, settings.Path, settings.Query);

        if (settings.Verbose)
        {
            AnsiConsole.MarkupLine($"[dim]{Method} {url}[/]");
        }

        using var httpClient = _clientFactory.CreateHttpClient(server, token, apiKey);

        // Custom headers
        if (settings.Headers is not null)
        {
            foreach (var header in settings.Headers)
            {
                var separatorIndex = header.IndexOf(':');
                if (separatorIndex > 0)
                {
                    var key = header[..separatorIndex].Trim();
                    var value = header[(separatorIndex + 1)..].Trim();
                    httpClient.DefaultRequestHeaders.TryAddWithoutValidation(key, value);
                }
            }
        }

        // Accept header
        if (!string.IsNullOrEmpty(settings.Accept))
        {
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue(settings.Accept));
        }

        // Build request
        var request = new HttpRequestMessage(Method, url);

        if (settings.Body is not null)
        {
            request.Content = new StringContent(settings.Body, Encoding.UTF8, "application/json");
        }

        // Execute
        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            CliErrorReporter.ReportNetworkException(context, settings, ex);
            return 1;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            CliErrorReporter.ReportUnexpectedException(context, settings, ex, "Unexpected error while executing the raw HTTP request.");
            return 1;
        }

        using (response)
        {
            if (settings.Verbose)
            {
                AnsiConsole.MarkupLine($"[dim]Status: {(int)response.StatusCode} {response.ReasonPhrase}[/]");
            }

            // Download mode
            if (!string.IsNullOrEmpty(settings.Download))
            {
                await using var fileStream = File.Create(settings.Download);
                await response.Content.CopyToAsync(fileStream, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    CliErrorReporter.ReportHttpFailure(context, settings, (int)response.StatusCode, response.ReasonPhrase);
                    return 1;
                }

                AnsiConsole.MarkupLine($"[green]Downloaded to [white]{settings.Download}[/].[/]");
                return 0;
            }

            // Read response body
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                CliErrorReporter.ReportHttpFailure(context, settings, (int)response.StatusCode, response.ReasonPhrase, body);
                return 1;
            }

            // Try to pretty-print JSON
            if (!string.IsNullOrWhiteSpace(body))
            {
                try
                {
                    using var doc = JsonDocument.Parse(body);
                    var formatted = JsonSerializer.Serialize(doc, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                    });
                    AnsiConsole.WriteLine(formatted);
                }
                catch (JsonException ex)
                {
                    var acceptLooksJson = settings.Accept?.Contains("json", StringComparison.OrdinalIgnoreCase) == true ||
                                          body.TrimStart().StartsWith('{') ||
                                          body.TrimStart().StartsWith('[');

                    if (acceptLooksJson)
                    {
                        CliErrorReporter.ReportUnexpectedException(
                            context,
                            settings,
                            ex,
                            "The server returned a body that looked like JSON, but the CLI could not parse it.");
                        AnsiConsole.MarkupLine("[dim]Raw body:[/]");
                    }

                    AnsiConsole.WriteLine(body);
                }
            }

            return 0;
        }
    }

    private (string? server, string? token, string? apiKey) ResolveCredentials(RawSettings settings)
    {
        var server = settings.Server;
        var token = settings.Token;
        var apiKey = settings.ApiKey;

        if (string.IsNullOrEmpty(server) || (string.IsNullOrEmpty(token) && string.IsNullOrEmpty(apiKey)))
        {
            var stored = _credentialStore.Load();
            if (stored is not null)
            {
                server ??= stored.Server;
                token ??= stored.Token;
                if (string.IsNullOrEmpty(token))
                    apiKey ??= stored.ApiKey;
            }
        }

        return (server, token, apiKey);
    }

    private static string BuildUrl(string server, string path, string[]? queryParams)
    {
        var baseUrl = server.TrimEnd('/');
        var apiPath = path.StartsWith('/') ? path : "/" + path;

        if (queryParams is null || queryParams.Length == 0)
        {
            return baseUrl + apiPath;
        }

        var queryString = string.Join("&", queryParams);
        var separator = apiPath.Contains('?') ? "&" : "?";
        return baseUrl + apiPath + separator + queryString;
    }
}

// ---------------------------------------------------------------------------
// GET
// ---------------------------------------------------------------------------

public sealed class RawGetCommand : RawCommandBase
{
    public RawGetCommand(ApiClientFactory clientFactory, CredentialStore credentialStore) : base(clientFactory, credentialStore)
    {
    }

    protected override HttpMethod Method => HttpMethod.Get;
}

// ---------------------------------------------------------------------------
// POST
// ---------------------------------------------------------------------------

public sealed class RawPostCommand : RawCommandBase
{
    public RawPostCommand(ApiClientFactory clientFactory, CredentialStore credentialStore) : base(clientFactory, credentialStore)
    {
    }

    protected override HttpMethod Method => HttpMethod.Post;
}

// ---------------------------------------------------------------------------
// PUT
// ---------------------------------------------------------------------------

public sealed class RawPutCommand : RawCommandBase
{
    public RawPutCommand(ApiClientFactory clientFactory, CredentialStore credentialStore) : base(clientFactory, credentialStore)
    {
    }

    protected override HttpMethod Method => HttpMethod.Put;
}

// ---------------------------------------------------------------------------
// DELETE
// ---------------------------------------------------------------------------

public sealed class RawDeleteCommand : RawCommandBase
{
    public RawDeleteCommand(ApiClientFactory clientFactory, CredentialStore credentialStore) : base(clientFactory, credentialStore)
    {
    }

    protected override HttpMethod Method => HttpMethod.Delete;
}

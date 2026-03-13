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
    private readonly CredentialStore _credentialStore;

    protected RawCommandBase(CredentialStore credentialStore)
    {
        _credentialStore = credentialStore;
    }

    protected abstract HttpMethod Method { get; }

    public override async Task<int> ExecuteAsync(
        CommandContext context, RawSettings settings, CancellationToken cancellationToken)
    {
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

        using var httpClient = new HttpClient();

        // Auth
        if (!string.IsNullOrEmpty(token))
        {
            httpClient.DefaultRequestHeaders.Add("Authorization", $"MediaBrowser Token=\"{token}\"");
        }
        else if (!string.IsNullOrEmpty(apiKey))
        {
            httpClient.DefaultRequestHeaders.Add("Authorization", $"MediaBrowser Token=\"{apiKey}\"");
        }

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
        using var response = await httpClient.SendAsync(
            request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (settings.Verbose)
        {
            AnsiConsole.MarkupLine($"[dim]Status: {(int)response.StatusCode} {response.ReasonPhrase}[/]");
        }

        // Download mode
        if (!string.IsNullOrEmpty(settings.Download))
        {
            await using var fileStream = File.Create(settings.Download);
            await response.Content.CopyToAsync(fileStream, cancellationToken);
            AnsiConsole.MarkupLine($"[green]Downloaded to [white]{settings.Download}[/].[/]");
            return response.IsSuccessStatusCode ? 0 : 1;
        }

        // Read response body
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            AnsiConsole.MarkupLine(
                $"[red]Error {(int)response.StatusCode}:[/] {response.ReasonPhrase}");
            if (!string.IsNullOrWhiteSpace(body))
            {
                AnsiConsole.WriteLine(body);
            }

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
            catch (JsonException)
            {
                AnsiConsole.WriteLine(body);
            }
        }

        return 0;
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
    public RawGetCommand(CredentialStore credentialStore) : base(credentialStore)
    {
    }

    protected override HttpMethod Method => HttpMethod.Get;
}

// ---------------------------------------------------------------------------
// POST
// ---------------------------------------------------------------------------

public sealed class RawPostCommand : RawCommandBase
{
    public RawPostCommand(CredentialStore credentialStore) : base(credentialStore)
    {
    }

    protected override HttpMethod Method => HttpMethod.Post;
}

// ---------------------------------------------------------------------------
// PUT
// ---------------------------------------------------------------------------

public sealed class RawPutCommand : RawCommandBase
{
    public RawPutCommand(CredentialStore credentialStore) : base(credentialStore)
    {
    }

    protected override HttpMethod Method => HttpMethod.Put;
}

// ---------------------------------------------------------------------------
// DELETE
// ---------------------------------------------------------------------------

public sealed class RawDeleteCommand : RawCommandBase
{
    public RawDeleteCommand(CredentialStore credentialStore) : base(credentialStore)
    {
    }

    protected override HttpMethod Method => HttpMethod.Delete;
}

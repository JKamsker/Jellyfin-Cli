using Jellyfin.Cli.Api.Generated;
using Microsoft.Kiota.Abstractions;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Common;

public abstract class ApiCommand<TSettings> : AsyncCommand<TSettings> where TSettings : GlobalSettings
{
    private readonly ApiClientFactory _clientFactory;
    private readonly CredentialStore _credentialStore;
    private string? _resolvedServer;
    private string? _resolvedToken;
    private string? _resolvedApiKey;
    private ResolvedContext? _resolvedContext;

    protected ApiCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
    {
        _clientFactory = clientFactory;
        _credentialStore = credentialStore;
    }

    protected CredentialStore CredentialStore => _credentialStore;
    protected string ResolvedServer => _resolvedServer ?? throw new InvalidOperationException("Resolved server is unavailable.");
    protected string? ResolvedToken => _resolvedToken;
    protected string? ResolvedApiKey => _resolvedApiKey;

    public override async Task<int> ExecuteAsync(CommandContext context, TSettings settings, CancellationToken cancellationToken)
    {
        HttpDiagnosticsContext.Clear();

        // Apply config path override before any config access
        _credentialStore.ConfigPathOverride = settings.ConfigPath
            ?? Environment.GetEnvironmentVariable("JF_CONFIG");

        var (server, token, apiKey) = ResolveCredentials(settings);
        _resolvedServer = server;
        _resolvedToken = token;
        _resolvedApiKey = apiKey;

        if (string.IsNullOrEmpty(server))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] No server specified. Use --server <URL> or run 'jf auth login --server <URL>'.");
            return 1;
        }

        // Resolve --user me to actual user ID
        if (string.Equals(settings.User, "me", StringComparison.OrdinalIgnoreCase))
        {
            var userId = _resolvedContext?.UserId;
            if (!string.IsNullOrEmpty(userId))
            {
                settings.User = userId;
            }
            else
            {
                // Fall back to API call
                var tempClient = _clientFactory.CreateClient(server, token, apiKey);
                try
                {
                    var me = await tempClient.Users.Me.GetAsync(cancellationToken: cancellationToken);
                    settings.User = me?.Id?.ToString();
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    var logPath = CliErrorReporter.WriteDiagnosticLog(
                        context,
                        settings,
                        ex,
                        "Failed while resolving '--user me'. The command continued without a user-scoped query.");

                    AnsiConsole.MarkupLine($"[yellow]Warning:[/] Could not resolve '--user me'. Proceeding without user context. {Markup.Escape(ex.Message)}");
                    if (HttpDiagnosticsContext.LastExchange?.Url is { } requestUrl)
                        AnsiConsole.MarkupLine($"[dim]Last request:[/] {Markup.Escape(requestUrl)}");
                    AnsiConsole.MarkupLine($"[dim]Diagnostic log:[/] {Markup.Escape(logPath)}");
                    settings.User = null;
                }
            }
        }

        var client = _clientFactory.CreateClient(server, token, apiKey);

        try
        {
            return await ExecuteAsync(context, settings, client, cancellationToken);
        }
        catch (ApiException ex) when (ex.ResponseStatusCode >= 400)
        {
            CliErrorReporter.ReportApiException(context, settings, ex);
            return 1;
        }
        catch (HttpRequestException ex)
        {
            CliErrorReporter.ReportNetworkException(context, settings, ex);
            return 1;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            CliErrorReporter.ReportUnexpectedException(
                context,
                settings,
                ex,
                "Unexpected client error while executing the Jellyfin API call.");
            return 1;
        }
    }

    protected abstract Task<int> ExecuteAsync(CommandContext context, TSettings settings, JellyfinApiClient client, CancellationToken cancellationToken);

    protected HttpClient CreateHttpClient()
    {
        return _clientFactory.CreateHttpClient(ResolvedServer, ResolvedToken, ResolvedApiKey);
    }

    protected async Task<Guid?> ResolveOptionalUserIdAsync(GlobalSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        if (Guid.TryParse(settings.User, out var userId))
            return userId;

        if (Guid.TryParse(_resolvedContext?.UserId, out userId))
            return userId;

        try
        {
            var me = await client.Users.Me.GetAsync(cancellationToken: cancellationToken);
            return me?.Id;
        }
        catch
        {
            return null;
        }
    }

    private (string? server, string? token, string? apiKey) ResolveCredentials(GlobalSettings settings)
    {
        // 1. CLI flags (highest priority)
        var server = settings.Server;
        var token = settings.Token;
        var apiKey = settings.ApiKey;

        // 2. Environment variables
        if (string.IsNullOrEmpty(server))
            server = Environment.GetEnvironmentVariable("JF_SERVER");
        if (string.IsNullOrEmpty(token))
            token = Environment.GetEnvironmentVariable("JF_TOKEN");
        if (string.IsNullOrEmpty(apiKey))
            apiKey = Environment.GetEnvironmentVariable("JF_API_KEY");

        // 3. Two-level profile resolution (host → profile)
        var profileName = settings.Profile ?? Environment.GetEnvironmentVariable("JF_PROFILE");
        var resolved = _credentialStore.Resolve(server, profileName);
        _resolvedContext = resolved;

        if (resolved is not null)
        {
            // Always use the resolved base URL — handles bare hostnames, profile overrides, etc.
            server = resolved.BaseUrl;
            if (string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(resolved.Token))
                token = resolved.Token;
            if (string.IsNullOrEmpty(token) && string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(resolved.ApiKey))
                apiKey = resolved.ApiKey;
        }
        else if (!string.IsNullOrEmpty(server) && !server.Contains("://", StringComparison.Ordinal))
        {
            // Bare hostname not in config — prepend https:// as best effort
            server = $"https://{server}";
        }

        return (server, token, apiKey);
    }
}

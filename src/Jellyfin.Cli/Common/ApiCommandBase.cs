using Jellyfin.Cli.Api.Generated;
using Microsoft.Kiota.Abstractions;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Common;

public abstract class ApiCommand<TSettings> : AsyncCommand<TSettings> where TSettings : GlobalSettings
{
    private readonly ApiClientFactory _clientFactory;
    private readonly CredentialStore _credentialStore;

    protected ApiCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
    {
        _clientFactory = clientFactory;
        _credentialStore = credentialStore;
    }

    protected CredentialStore CredentialStore => _credentialStore;

    public override async Task<int> ExecuteAsync(CommandContext context, TSettings settings, CancellationToken cancellationToken)
    {
        var (server, token, apiKey) = ResolveCredentials(settings);

        if (string.IsNullOrEmpty(server))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] No server URL. Use --server or run 'jf auth login'.");
            return 1;
        }

        // Resolve --user me to actual user ID
        if (string.Equals(settings.User, "me", StringComparison.OrdinalIgnoreCase))
        {
            var stored = _credentialStore.Load();
            if (!string.IsNullOrEmpty(stored?.UserId))
            {
                settings.User = stored.UserId;
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
                catch
                {
                    AnsiConsole.MarkupLine("[yellow]Warning:[/] Could not resolve '--user me'. Proceeding without user context.");
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
            var status = ex.ResponseStatusCode;
            var message = status switch
            {
                401 => "Authentication required. Run 'jf auth login' first.",
                403 => "Permission denied. This may require admin privileges.",
                404 => "Resource not found.",
                _ => ex.Message,
            };
            AnsiConsole.MarkupLine($"[red]Error {status}:[/] {Markup.Escape(message)}");
            if (settings.Verbose && !string.IsNullOrEmpty(ex.Message))
                AnsiConsole.MarkupLine($"[dim]{Markup.Escape(ex.Message)}[/]");
            return 1;
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.MarkupLine($"[red]Network error:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }

    protected abstract Task<int> ExecuteAsync(CommandContext context, TSettings settings, JellyfinApiClient client, CancellationToken cancellationToken);

    private (string? server, string? token, string? apiKey) ResolveCredentials(GlobalSettings settings)
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
}

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Auth;

public sealed class LogoutCommand : ApiCommand<GlobalSettings>
{
    private readonly CredentialStore _credentialStore;

    public LogoutCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
        _credentialStore = credentialStore;
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, GlobalSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        try
        {
            await client.Sessions.Logout.PostAsync();
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]Warning:[/] Server logout failed: {ex.Message}");
        }

        // Use the context resolved by the base class (matches the host/profile used for the API call)
        var resolved = ResolvedContext;

        if (resolved is not null)
        {
            _credentialStore.DeleteProfile(resolved.Hostname, resolved.ProfileName);
            AnsiConsole.MarkupLine($"[green]Logged out and removed profile '{Markup.Escape(resolved.ProfileName)}' from host '{Markup.Escape(resolved.Hostname)}'.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]No profile to remove.[/]");
        }

        return 0;
    }
}

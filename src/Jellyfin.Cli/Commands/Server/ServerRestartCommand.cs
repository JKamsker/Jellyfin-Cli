using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Server;

public sealed class ServerRestartSettings : GlobalSettings
{
}

public sealed class ServerRestartCommand : ApiCommand<ServerRestartSettings>
{
    public ServerRestartCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ServerRestartSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        if (!settings.Yes)
        {
            var confirmed = OutputHelper.Confirm(
                "Are you sure you want to restart the server?");

            if (!confirmed)
            {
                AnsiConsole.MarkupLine("[yellow]Aborted.[/]");
                return 0;
            }
        }

        await client.System.Restart.PostAsync();

        AnsiConsole.MarkupLine("[green]Server restart initiated.[/]");
        return 0;
    }
}

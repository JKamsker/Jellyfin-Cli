using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Server;

public sealed class ServerShutdownSettings : GlobalSettings
{
}

public sealed class ServerShutdownCommand : ApiCommand<ServerShutdownSettings>
{
    public ServerShutdownCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ServerShutdownSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        if (!settings.Yes)
        {
            var confirmed = OutputHelper.Confirm(
                "Are you sure you want to shut down the server?");

            if (!confirmed)
            {
                AnsiConsole.MarkupLine("[yellow]Aborted.[/]");
                return 0;
            }
        }

        await client.System.Shutdown.PostAsync();

        AnsiConsole.MarkupLine("[green]Server shutdown initiated.[/]");
        return 0;
    }
}

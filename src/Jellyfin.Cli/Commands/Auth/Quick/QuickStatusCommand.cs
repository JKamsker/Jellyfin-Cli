using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Auth.Quick;

public sealed class QuickStatusCommand : ApiCommand<GlobalSettings>
{
    public QuickStatusCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, GlobalSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var enabled = await client.QuickConnect.Enabled.GetAsync();

        if (settings.Json)
        {
            OutputHelper.WriteJson(new { enabled });
            return 0;
        }

        if (enabled == true)
        {
            AnsiConsole.MarkupLine("[green]Quick Connect is enabled.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]Quick Connect is disabled.[/]");
        }

        return 0;
    }
}

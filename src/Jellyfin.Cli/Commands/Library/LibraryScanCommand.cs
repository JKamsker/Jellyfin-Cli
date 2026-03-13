using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Library;

public sealed class LibraryScanCommand : ApiCommand<GlobalSettings>
{
    public LibraryScanCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, GlobalSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        await client.Library.Refresh.PostAsync();

        AnsiConsole.MarkupLine("[green]Library scan started.[/]");
        return 0;
    }
}

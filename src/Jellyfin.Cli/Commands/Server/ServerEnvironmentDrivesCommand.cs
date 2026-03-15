using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Server;

public sealed class ServerEnvironmentDrivesCommand : ApiCommand<GlobalSettings>
{
    public ServerEnvironmentDrivesCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, GlobalSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var drives = await client.Environment.Drives.GetAsync(cancellationToken: cancellationToken) ?? [];

        if (drives.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No drives found.[/]");
            return 0;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(drives);
            return 0;
        }

        var table = OutputHelper.CreateTable("Type", "Name", "Path");
        foreach (var drive in drives.OrderBy(d => d.Path ?? d.Name))
        {
            table.AddRow(
                drive.Type?.ToString() ?? string.Empty,
                Markup.Escape(drive.Name ?? string.Empty),
                Markup.Escape(drive.Path ?? string.Empty));
        }

        OutputHelper.WriteTable(table);
        return 0;
    }
}

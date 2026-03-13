using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Backups;

public sealed class BackupsListCommand : ApiCommand<GlobalSettings>
{
    public BackupsListCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, GlobalSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var backups = await client.Backup.GetAsync();

        if (backups is null || backups.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No backups found.[/]");
            return 0;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(backups.Select(b => new
            {
                path = b.Path,
                dateCreated = b.DateCreated,
                serverVersion = b.ServerVersion,
                backupEngineVersion = b.BackupEngineVersion,
            }));
            return 0;
        }

        var table = OutputHelper.CreateTable("Path", "DateCreated", "ServerVersion", "EngineVersion");
        foreach (var backup in backups)
        {
            table.AddRow(
                backup.Path ?? "(unknown)",
                backup.DateCreated?.ToString("u") ?? "(unknown)",
                backup.ServerVersion ?? "(unknown)",
                backup.BackupEngineVersion ?? "(unknown)");
        }

        OutputHelper.WriteTable(table);
        return 0;
    }
}

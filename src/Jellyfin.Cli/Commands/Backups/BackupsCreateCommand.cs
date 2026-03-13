using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Api.Generated.Models;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Backups;

public sealed class BackupsCreateSettings : GlobalSettings
{
    [CommandOption("--metadata")]
    [Description("Include metadata in the backup")]
    public bool Metadata { get; set; }

    [CommandOption("--trickplay")]
    [Description("Include trickplay data in the backup")]
    public bool Trickplay { get; set; }

    [CommandOption("--subtitles")]
    [Description("Include subtitles in the backup")]
    public bool Subtitles { get; set; }

    [CommandOption("--database")]
    [Description("Include database in the backup")]
    public bool Database { get; set; }
}

public sealed class BackupsCreateCommand : ApiCommand<BackupsCreateSettings>
{
    public BackupsCreateCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, BackupsCreateSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var options = new BackupOptionsDto
        {
            Metadata = settings.Metadata,
            Trickplay = settings.Trickplay,
            Subtitles = settings.Subtitles,
            Database = settings.Database,
        };

        AnsiConsole.MarkupLine("[blue]Creating backup...[/]");

        var result = await client.Backup.Create.PostAsync(options);

        if (result is null)
        {
            AnsiConsole.MarkupLine("[red]Backup creation failed: no response received.[/]");
            return 1;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(new
            {
                path = result.Path,
                dateCreated = result.DateCreated,
                serverVersion = result.ServerVersion,
                backupEngineVersion = result.BackupEngineVersion,
            });
            return 0;
        }

        var table = OutputHelper.CreateTable("Field", "Value");
        table.AddRow("Path", result.Path ?? "(unknown)");
        table.AddRow("Date Created", result.DateCreated?.ToString("u") ?? "(unknown)");
        table.AddRow("Server Version", result.ServerVersion ?? "(unknown)");
        table.AddRow("Engine Version", result.BackupEngineVersion ?? "(unknown)");
        OutputHelper.WriteTable(table);

        AnsiConsole.MarkupLine("[green]Backup created successfully.[/]");
        return 0;
    }
}

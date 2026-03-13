using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Api.Generated.Models;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Backups;

public sealed class BackupsRestoreSettings : GlobalSettings
{
    [CommandArgument(0, "<ARCHIVE_FILE>")]
    [Description("The backup archive file name to restore from")]
    public string ArchiveFileName { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(ArchiveFileName))
            return ValidationResult.Error("ARCHIVE_FILE is required.");

        return ValidationResult.Success();
    }
}

public sealed class BackupsRestoreCommand : ApiCommand<BackupsRestoreSettings>
{
    public BackupsRestoreCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, BackupsRestoreSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        if (!settings.Yes)
        {
            AnsiConsole.MarkupLine(
                "[yellow]Warning:[/] Restoring a backup will restart the Jellyfin server.");

            if (!OutputHelper.Confirm($"Restore from '{settings.ArchiveFileName}'?"))
            {
                AnsiConsole.MarkupLine("[yellow]Cancelled.[/]");
                return 0;
            }
        }

        var request = new BackupRestoreRequestDto
        {
            ArchiveFileName = settings.ArchiveFileName,
        };

        await client.Backup.Restore.PostAsync(request);

        AnsiConsole.MarkupLine(
            "[green]Backup restore initiated. The server will restart to apply the backup.[/]");
        return 0;
    }
}

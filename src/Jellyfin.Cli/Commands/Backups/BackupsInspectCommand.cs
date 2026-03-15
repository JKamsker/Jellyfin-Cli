using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Backups;

public sealed class BackupsInspectSettings : GlobalSettings
{
    [CommandArgument(0, "<BACKUP_PATH>")]
    [Description("Backup archive path")]
    public string BackupPath { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        return string.IsNullOrWhiteSpace(BackupPath)
            ? ValidationResult.Error("A backup archive path is required.")
            : ValidationResult.Success();
    }
}

public sealed class BackupsInspectCommand : ApiCommand<BackupsInspectSettings>
{
    public BackupsInspectCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, BackupsInspectSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var manifest = await client.Backup.Manifest.GetAsync(config =>
        {
            config.QueryParameters.Path = settings.BackupPath;
        }, cancellationToken);

        if (manifest is null)
        {
            AnsiConsole.MarkupLine("[yellow]No backup manifest returned.[/]");
            return 0;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(manifest);
            return 0;
        }

        var summary = OutputHelper.CreateTable("Field", "Value");
        summary.AddRow("Path", manifest.Path ?? settings.BackupPath);
        summary.AddRow("Date Created", manifest.DateCreated?.ToString("u") ?? string.Empty);
        summary.AddRow("Server Version", manifest.ServerVersion ?? string.Empty);
        summary.AddRow("Backup Engine Version", manifest.BackupEngineVersion ?? string.Empty);
        OutputHelper.WriteTable(summary);

        if (manifest.Options is not null)
        {
            AnsiConsole.WriteLine();
            OutputHelper.WriteJson(manifest.Options);
        }

        return 0;
    }
}

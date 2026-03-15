using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Api.Generated.Models;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Library;

public sealed class LibraryOptionsSettings : GlobalSettings
{
    [CommandArgument(0, "<FOLDER>")]
    [Description("Virtual folder name")]
    public string Folder { get; set; } = string.Empty;

    [CommandOption("--file <FILE>")]
    [Description("JSON file with LibraryOptions")]
    public string? FilePath { get; set; }

    [CommandOption("--data <JSON>")]
    [Description("Inline JSON with LibraryOptions")]
    public string? InlineJson { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Folder))
            return ValidationResult.Error("A virtual folder name is required.");

        var hasFile = !string.IsNullOrWhiteSpace(FilePath);
        var hasInline = !string.IsNullOrWhiteSpace(InlineJson);
        return hasFile && hasInline
            ? ValidationResult.Error("Specify exactly one of --file or --data.")
            : ValidationResult.Success();
    }
}

public sealed class LibraryOptionsCommand : ApiCommand<LibraryOptionsSettings>
{
    public LibraryOptionsCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, LibraryOptionsSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var folder = await LibraryCommandHelper.GetFolderByNameAsync(client, settings.Folder, cancellationToken);
        var hasUpdatePayload = !string.IsNullOrWhiteSpace(settings.FilePath) || !string.IsNullOrWhiteSpace(settings.InlineJson);

        if (!hasUpdatePayload)
        {
            if (settings.Json)
            {
                OutputHelper.WriteJson(folder.LibraryOptions);
                return 0;
            }

            var summary = OutputHelper.CreateTable("Field", "Value");
            summary.AddRow("Folder", Markup.Escape(folder.Name ?? settings.Folder));
            summary.AddRow("Collection Type", folder.CollectionType?.ToString() ?? string.Empty);
            summary.AddRow("Item ID", folder.ItemId ?? string.Empty);
            summary.AddRow("Paths", string.Join(", ", folder.Locations ?? []));
            OutputHelper.WriteTable(summary);

            AnsiConsole.WriteLine();
            OutputHelper.WriteJson(folder.LibraryOptions);
            return 0;
        }

        if (!Guid.TryParse(folder.ItemId, out var folderId))
        {
            AnsiConsole.MarkupLine("[red]The selected virtual folder does not expose a valid item ID for updating options.[/]");
            return 1;
        }

        var libraryOptions = JsonCommandHelper.DeserializeFromFileOrInline<LibraryOptions>(
            settings.FilePath,
            settings.InlineJson,
            "--file/--data");

        await client.Library.VirtualFolders.LibraryOptions.PostAsync(new UpdateLibraryOptionsDto
        {
            Id = folderId,
            LibraryOptions = libraryOptions,
        }, cancellationToken: cancellationToken);

        AnsiConsole.MarkupLine(
            $"[green]Updated library options for [white]{Markup.Escape(folder.Name ?? settings.Folder)}[/].[/]");
        return 0;
    }
}

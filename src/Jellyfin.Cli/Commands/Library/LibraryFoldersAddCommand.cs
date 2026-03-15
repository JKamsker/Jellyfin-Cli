using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Api.Generated.Models;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Library;

public sealed class LibraryFoldersAddSettings : GlobalSettings
{
    [CommandArgument(0, "<NAME>")]
    [Description("Virtual folder name")]
    public string Name { get; set; } = string.Empty;

    [CommandOption("--type <TYPE>")]
    [Description("Collection type: movies, tvshows, music, musicvideos, homevideos, boxsets, books, mixed")]
    public string? CollectionType { get; set; }

    [CommandOption("--path <PATH>")]
    [Description("Media path, repeat for multiple paths")]
    public string[]? Paths { get; set; }

    [CommandOption("--options-file <FILE>")]
    [Description("JSON file with LibraryOptions")]
    public string? OptionsFile { get; set; }

    [CommandOption("--options-data <JSON>")]
    [Description("Inline JSON with LibraryOptions")]
    public string? OptionsJson { get; set; }

    [CommandOption("--refresh")]
    [Description("Refresh the library after creating the folder")]
    public bool RefreshLibrary { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return ValidationResult.Error("A virtual folder name is required.");

        if (string.IsNullOrWhiteSpace(CollectionType))
            return ValidationResult.Error("--type is required.");

        if (Paths is null || Paths.Length == 0 || Paths.Any(string.IsNullOrWhiteSpace))
            return ValidationResult.Error("Specify at least one --path value.");

        var hasFile = !string.IsNullOrWhiteSpace(OptionsFile);
        var hasInline = !string.IsNullOrWhiteSpace(OptionsJson);
        if (hasFile && hasInline)
            return ValidationResult.Error("Specify exactly one of --options-file or --options-data.");

        return ValidationResult.Success();
    }
}

public sealed class LibraryFoldersAddCommand : ApiCommand<LibraryFoldersAddSettings>
{
    public LibraryFoldersAddCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, LibraryFoldersAddSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var body = new AddVirtualFolderDto();
        if (!string.IsNullOrWhiteSpace(settings.OptionsFile) || !string.IsNullOrWhiteSpace(settings.OptionsJson))
        {
            body.LibraryOptions = JsonCommandHelper.DeserializeFromFileOrInline<LibraryOptions>(
                settings.OptionsFile,
                settings.OptionsJson,
                "--options-file/--options-data");
        }

        await client.Library.VirtualFolders.PostAsync(body, config =>
        {
            config.QueryParameters.Name = settings.Name;
            config.QueryParameters.Paths = settings.Paths;
            config.QueryParameters.CollectionTypeAsCollectionTypeOptions =
                LibraryCommandHelper.ParseCollectionType(settings.CollectionType!);
            config.QueryParameters.RefreshLibrary = settings.RefreshLibrary ? true : null;
        }, cancellationToken);

        AnsiConsole.MarkupLine($"[green]Created virtual folder [white]{Markup.Escape(settings.Name)}[/].[/]");
        return 0;
    }
}

using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Library;

public sealed class LibraryFoldersRenameSettings : GlobalSettings
{
    [CommandArgument(0, "<NAME>")]
    [Description("Existing virtual folder name")]
    public string Name { get; set; } = string.Empty;

    [CommandArgument(1, "<NEW_NAME>")]
    [Description("New virtual folder name")]
    public string NewName { get; set; } = string.Empty;

    [CommandOption("--refresh")]
    [Description("Refresh the library after renaming")]
    public bool RefreshLibrary { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return ValidationResult.Error("A virtual folder name is required.");

        return string.IsNullOrWhiteSpace(NewName)
            ? ValidationResult.Error("A new virtual folder name is required.")
            : ValidationResult.Success();
    }
}

public sealed class LibraryFoldersRenameCommand : ApiCommand<LibraryFoldersRenameSettings>
{
    public LibraryFoldersRenameCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, LibraryFoldersRenameSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        await client.Library.VirtualFolders.Name.PostAsync(config =>
        {
            config.QueryParameters.Name = settings.Name;
            config.QueryParameters.NewName = settings.NewName;
            config.QueryParameters.RefreshLibrary = settings.RefreshLibrary ? true : null;
        }, cancellationToken);

        AnsiConsole.MarkupLine(
            $"[green]Renamed virtual folder [white]{Markup.Escape(settings.Name)}[/] to [white]{Markup.Escape(settings.NewName)}[/].[/]");
        return 0;
    }
}

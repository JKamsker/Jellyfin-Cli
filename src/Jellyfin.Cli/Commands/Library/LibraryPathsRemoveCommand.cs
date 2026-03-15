using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Library;

public sealed class LibraryPathsRemoveSettings : GlobalSettings
{
    [CommandArgument(0, "<FOLDER>")]
    [Description("Virtual folder name")]
    public string Folder { get; set; } = string.Empty;

    [CommandArgument(1, "<PATH>")]
    [Description("Media path to remove")]
    public string Path { get; set; } = string.Empty;

    [CommandOption("--refresh")]
    [Description("Refresh the library after removing the path")]
    public bool RefreshLibrary { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Folder))
            return ValidationResult.Error("A virtual folder name is required.");

        return string.IsNullOrWhiteSpace(Path)
            ? ValidationResult.Error("A media path is required.")
            : ValidationResult.Success();
    }
}

public sealed class LibraryPathsRemoveCommand : ApiCommand<LibraryPathsRemoveSettings>
{
    public LibraryPathsRemoveCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, LibraryPathsRemoveSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        if (!settings.Yes)
        {
            var confirmed = OutputHelper.Confirm(
                $"Remove media path '{settings.Path}' from '{settings.Folder}'?");
            if (!confirmed)
            {
                AnsiConsole.MarkupLine("[yellow]Cancelled.[/]");
                return 0;
            }
        }

        await client.Library.VirtualFolders.Paths.DeleteAsync(config =>
        {
            config.QueryParameters.Name = settings.Folder;
            config.QueryParameters.Path = settings.Path;
            config.QueryParameters.RefreshLibrary = settings.RefreshLibrary ? true : null;
        }, cancellationToken);

        AnsiConsole.MarkupLine(
            $"[green]Removed media path [white]{Markup.Escape(settings.Path)}[/] from [white]{Markup.Escape(settings.Folder)}[/].[/]");
        return 0;
    }
}

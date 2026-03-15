using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Api.Generated.Models;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Library;

public sealed class LibraryPathsAddSettings : GlobalSettings
{
    [CommandArgument(0, "<FOLDER>")]
    [Description("Virtual folder name")]
    public string Folder { get; set; } = string.Empty;

    [CommandArgument(1, "<PATH>")]
    [Description("Media path to add")]
    public string Path { get; set; } = string.Empty;

    [CommandOption("--refresh")]
    [Description("Refresh the library after adding the path")]
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

public sealed class LibraryPathsAddCommand : ApiCommand<LibraryPathsAddSettings>
{
    public LibraryPathsAddCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, LibraryPathsAddSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        await client.Library.VirtualFolders.Paths.PostAsync(new MediaPathDto
        {
            Name = settings.Folder,
            Path = settings.Path,
            PathInfo = new MediaPathInfo
            {
                Path = settings.Path,
            },
        }, config =>
        {
            config.QueryParameters.RefreshLibrary = settings.RefreshLibrary ? true : null;
        }, cancellationToken);

        AnsiConsole.MarkupLine(
            $"[green]Added media path [white]{Markup.Escape(settings.Path)}[/] to [white]{Markup.Escape(settings.Folder)}[/].[/]");
        return 0;
    }
}

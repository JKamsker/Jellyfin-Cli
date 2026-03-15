using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Api.Generated.Models;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Library;

public sealed class LibraryPathsUpdateSettings : GlobalSettings
{
    [CommandArgument(0, "<FOLDER>")]
    [Description("Virtual folder name")]
    public string Folder { get; set; } = string.Empty;

    [CommandArgument(1, "<PATH>")]
    [Description("New media path value")]
    public string Path { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Folder))
            return ValidationResult.Error("A virtual folder name is required.");

        return string.IsNullOrWhiteSpace(Path)
            ? ValidationResult.Error("A media path is required.")
            : ValidationResult.Success();
    }
}

public sealed class LibraryPathsUpdateCommand : ApiCommand<LibraryPathsUpdateSettings>
{
    public LibraryPathsUpdateCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, LibraryPathsUpdateSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        await client.Library.VirtualFolders.Paths.Update.PostAsync(new UpdateMediaPathRequestDto
        {
            Name = settings.Folder,
            PathInfo = new MediaPathInfo
            {
                Path = settings.Path,
            },
        }, cancellationToken: cancellationToken);

        AnsiConsole.MarkupLine(
            $"[green]Updated media path entry for [white]{Markup.Escape(settings.Folder)}[/] to [white]{Markup.Escape(settings.Path)}[/].[/]");
        return 0;
    }
}

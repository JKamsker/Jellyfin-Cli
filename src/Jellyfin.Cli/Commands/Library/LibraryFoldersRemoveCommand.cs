using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Library;

public sealed class LibraryFoldersRemoveSettings : GlobalSettings
{
    [CommandArgument(0, "<NAME>")]
    [Description("Virtual folder name")]
    public string Name { get; set; } = string.Empty;

    [CommandOption("--refresh")]
    [Description("Refresh the library after removing the folder")]
    public bool RefreshLibrary { get; set; }

    public override ValidationResult Validate()
    {
        return string.IsNullOrWhiteSpace(Name)
            ? ValidationResult.Error("A virtual folder name is required.")
            : ValidationResult.Success();
    }
}

public sealed class LibraryFoldersRemoveCommand : ApiCommand<LibraryFoldersRemoveSettings>
{
    public LibraryFoldersRemoveCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, LibraryFoldersRemoveSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        if (!settings.Yes)
        {
            var confirmed = OutputHelper.Confirm($"Remove virtual folder '{settings.Name}'?");
            if (!confirmed)
            {
                AnsiConsole.MarkupLine("[yellow]Cancelled.[/]");
                return 0;
            }
        }

        await client.Library.VirtualFolders.DeleteAsync(config =>
        {
            config.QueryParameters.Name = settings.Name;
            config.QueryParameters.RefreshLibrary = settings.RefreshLibrary ? true : null;
        }, cancellationToken);

        AnsiConsole.MarkupLine($"[green]Removed virtual folder [white]{Markup.Escape(settings.Name)}[/].[/]");
        return 0;
    }
}

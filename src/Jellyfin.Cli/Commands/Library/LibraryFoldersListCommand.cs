using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Library;

public sealed class LibraryFoldersListCommand : ApiCommand<GlobalSettings>
{
    public LibraryFoldersListCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, GlobalSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var folders = await client.Library.VirtualFolders.GetAsync();

        if (folders is null || folders.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No virtual folders found.[/]");
            return 0;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(folders.Select(f => new
            {
                name = f.Name,
                collectionType = f.CollectionType?.ToString(),
                paths = f.Locations,
                itemId = f.ItemId,
            }));
            return 0;
        }

        var table = OutputHelper.CreateTable("Name", "CollectionType", "Paths", "ItemId");
        foreach (var folder in folders)
        {
            var paths = folder.Locations is { Count: > 0 }
                ? string.Join(", ", folder.Locations)
                : "(none)";

            table.AddRow(
                folder.Name ?? "(unknown)",
                folder.CollectionType?.ToString() ?? "(unknown)",
                paths,
                folder.ItemId ?? "(unknown)");
        }

        OutputHelper.WriteTable(table);
        return 0;
    }
}

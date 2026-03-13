using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Library;

public sealed class LibraryMediaCommand : ApiCommand<GlobalSettings>
{
    public LibraryMediaCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, GlobalSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var result = await client.Library.MediaFolders.GetAsync();

        var items = result?.Items;
        if (items is null || items.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No media folders found.[/]");
            return 0;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(items.Select(f => new
            {
                name = f.Name,
                id = f.Id,
                collectionType = f.CollectionType?.ToString(),
                path = f.Path,
                type = f.Type?.ToString(),
            }));
            return 0;
        }

        var table = OutputHelper.CreateTable("Name", "ID", "CollectionType", "Path", "Type");
        foreach (var folder in items)
        {
            table.AddRow(
                folder.Name ?? "(unknown)",
                folder.Id?.ToString() ?? "(unknown)",
                folder.CollectionType?.ToString() ?? "(unknown)",
                folder.Path ?? "(unknown)",
                folder.Type?.ToString() ?? "(unknown)");
        }

        OutputHelper.WriteTable(table);
        return 0;
    }
}

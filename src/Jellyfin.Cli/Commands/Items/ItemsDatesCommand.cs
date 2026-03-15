using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Items;

public sealed class ItemsDatesSettings : GlobalSettings
{
    [CommandArgument(0, "<ID>")]
    [Description("Item ID")]
    public string Id { get; set; } = string.Empty;

    [CommandOption("--recursive")]
    [Description("Include seasons and episodes beneath the item")]
    public bool Recursive { get; set; }

    public override ValidationResult Validate()
    {
        return Guid.TryParse(Id, out _)
            ? ValidationResult.Success()
            : ValidationResult.Error("A valid item ID is required.");
    }
}

public sealed class ItemsDatesCommand : ApiCommand<ItemsDatesSettings>
{
    public ItemsDatesCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ItemsDatesSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var itemId = Guid.Parse(settings.Id);
        var userId = await ResolveOptionalUserIdAsync(settings, client, cancellationToken);
        var item = await ItemDiagnosticHelpers.GetDiagnosticItemAsync(client, itemId, userId, cancellationToken);

        if (item is null)
        {
            AnsiConsole.MarkupLine("[red]Item not found.[/]");
            return 1;
        }

        using var httpClient = CreateHttpClient();

        if (!settings.Recursive)
        {
            var rawJson = await ItemDiagnosticHelpers.GetItemJsonAsync(httpClient, itemId, userId, cancellationToken);
            var dateModified = ItemDiagnosticHelpers.TryGetDateTimeOffset(rawJson?.RootElement, "DateModified");

            if (settings.Json)
            {
                OutputHelper.WriteJson(new
                {
                    id = item.Id,
                    name = item.Name,
                    type = item.Type?.ToString(),
                    dateCreated = item.DateCreated,
                    dateModified,
                    dateLastMediaAdded = item.DateLastMediaAdded,
                    premiereDate = item.PremiereDate,
                    path = item.Path,
                    locationType = item.LocationType?.ToString(),
                });
                return 0;
            }

            var table = OutputHelper.CreateTable("Field", "Value");
            table.AddRow("Id", item.Id?.ToString() ?? string.Empty);
            table.AddRow("Name", Markup.Escape(item.Name ?? "(untitled)"));
            table.AddRow("Type", item.Type?.ToString() ?? string.Empty);
            table.AddRow("DateCreated", ItemDiagnosticHelpers.FormatDate(item.DateCreated));
            table.AddRow("DateModified", ItemDiagnosticHelpers.FormatDate(dateModified));
            table.AddRow("DateLastMediaAdded", ItemDiagnosticHelpers.FormatDate(item.DateLastMediaAdded));
            table.AddRow("PremiereDate", ItemDiagnosticHelpers.FormatDate(item.PremiereDate));
            table.AddRow("LocationType", item.LocationType?.ToString() ?? string.Empty);
            table.AddRow("Path", Markup.Escape(item.Path ?? string.Empty));

            OutputHelper.WriteTable(table);
            return 0;
        }

        var entries = await ItemDiagnosticHelpers.BuildTreeAsync(client, httpClient, item, userId, includeDateModified: true, cancellationToken);

        if (settings.Json)
        {
            OutputHelper.WriteJson(new
            {
                rootId = item.Id,
                items = entries.Select(entry => new
                {
                    depth = entry.Depth,
                    id = entry.Item.Id,
                    name = entry.Item.Name,
                    type = entry.Item.Type?.ToString(),
                    dateCreated = entry.Item.DateCreated,
                    dateModified = entry.DateModified,
                    dateLastMediaAdded = entry.Item.DateLastMediaAdded,
                    premiereDate = entry.Item.PremiereDate,
                    path = entry.Item.Path,
                    locationType = entry.Item.LocationType?.ToString(),
                }),
            });
            return 0;
        }

        var recursiveTable = OutputHelper.CreateTable("Node", "Type", "DateCreated", "DateModified", "DateLastMediaAdded", "PremiereDate", "Path");
        foreach (var entry in entries)
        {
            recursiveTable.AddRow(
                ItemDiagnosticHelpers.FormatNodeLabel(entry.Item, entry.Depth),
                entry.Item.Type?.ToString() ?? string.Empty,
                ItemDiagnosticHelpers.FormatDate(entry.Item.DateCreated),
                ItemDiagnosticHelpers.FormatDate(entry.DateModified),
                ItemDiagnosticHelpers.FormatDate(entry.Item.DateLastMediaAdded),
                ItemDiagnosticHelpers.FormatDate(entry.Item.PremiereDate),
                Markup.Escape(entry.Item.Path ?? string.Empty));
        }

        OutputHelper.WriteTable(recursiveTable);
        return 0;
    }
}

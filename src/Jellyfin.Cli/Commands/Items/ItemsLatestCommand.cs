using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Api.Generated.Models;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Items;

public sealed class ItemsLatestSettings : GlobalSettings
{
    [CommandOption("--parent <ID>")]
    [Description("Parent folder ID to scope the latest shelf")]
    public string? Parent { get; set; }

    [CommandOption("--type <TYPE>")]
    [Description("Include only item types, comma-separated (e.g. Movie,Series,Episode)")]
    public string? ItemType { get; set; }

    [CommandOption("--played")]
    [Description("Show only played items")]
    public bool Played { get; set; }

    [CommandOption("--unplayed")]
    [Description("Show only unplayed items")]
    public bool Unplayed { get; set; }

    [CommandOption("--ungrouped")]
    [Description("Disable Jellyfin's parent-container grouping")]
    public bool Ungrouped { get; set; }

    public override ValidationResult Validate()
    {
        if (!string.IsNullOrWhiteSpace(Parent) && !Guid.TryParse(Parent, out _))
            return ValidationResult.Error("A valid parent ID is required.");

        if (Played && Unplayed)
            return ValidationResult.Error("Use either --played or --unplayed, not both.");

        return ValidationResult.Success();
    }
}

public sealed class ItemsLatestCommand : ApiCommand<ItemsLatestSettings>
{
    public ItemsLatestCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ItemsLatestSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var userId = await ResolveOptionalUserIdAsync(settings, client, cancellationToken);
        var items = await client.Items.Latest.GetAsync(config =>
        {
            var q = config.QueryParameters;
            q.UserId = userId;
            q.Limit = settings.Limit ?? 20;
            q.GroupItems = !settings.Ungrouped;
            q.EnableUserData = userId is not null;
            q.FieldsAsItemFields = ItemDiagnosticHelpers.DiagnosticFields;

            if (Guid.TryParse(settings.Parent, out var parentId))
                q.ParentId = parentId;

            if (!string.IsNullOrWhiteSpace(settings.ItemType))
            {
                q.IncludeItemTypesAsBaseItemKind = settings.ItemType
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .Select(ParseItemType)
                    .ToArray();
            }

            if (settings.Played)
                q.IsPlayed = true;
            else if (settings.Unplayed)
                q.IsPlayed = false;
        }, cancellationToken) ?? [];

        if (items.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No latest items found.[/]");
            return 0;
        }

        var projected = items
            .Select((item, index) => new
            {
                rank = index + 1,
                id = item.Id,
                name = item.Name,
                type = item.Type?.ToString(),
                containerId = ItemDiagnosticHelpers.GetLatestContainerId(item),
                container = ItemDiagnosticHelpers.DescribeLatestContainer(item),
                dateCreated = item.DateCreated,
                dateLastMediaAdded = item.DateLastMediaAdded,
                played = item.UserData?.Played,
                path = item.Path,
            })
            .ToList();

        if (settings.Json)
        {
            OutputHelper.WriteJson(new
            {
                grouped = !settings.Ungrouped,
                userId,
                parentId = Guid.TryParse(settings.Parent, out var parentId) ? (Guid?)parentId : null,
                count = projected.Count,
                items = projected,
            });
            return 0;
        }

        var table = OutputHelper.CreateTable("#", "Id", "Name", "Type", "Container", "DateCreated", "DateLastMediaAdded", "Played");
        foreach (var item in projected)
        {
            table.AddRow(
                item.rank.ToString(),
                item.id?.ToString() ?? string.Empty,
                Markup.Escape(item.name ?? "(untitled)"),
                item.type ?? string.Empty,
                Markup.Escape(item.container),
                ItemDiagnosticHelpers.FormatDate(item.dateCreated),
                ItemDiagnosticHelpers.FormatDate(item.dateLastMediaAdded),
                item.played?.ToString() ?? string.Empty);
        }

        OutputHelper.WriteTable(table);
        AnsiConsole.MarkupLine($"[dim]Showing {projected.Count} latest items (grouped: {!settings.Ungrouped}).[/]");
        return 0;
    }

    private static BaseItemKind ParseItemType(string value)
    {
        return Enum.TryParse<BaseItemKind>(value, true, out var parsed)
            ? parsed
            : throw new InvalidOperationException($"Unknown item type '{value}'.");
    }
}

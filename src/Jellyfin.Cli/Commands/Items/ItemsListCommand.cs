using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Api.Generated.Models;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Items;

public sealed class ItemsListSettings : GlobalSettings
{
    [CommandOption("--search <TERM>")]
    [Description("Filter by search term")]
    public string? Search { get; set; }

    [CommandOption("--parent <ID>")]
    [Description("Parent folder ID to scope the query")]
    public string? Parent { get; set; }

    [CommandOption("--sort <FIELD>")]
    [Description("Sort field (e.g. SortName, DateCreated, PremiereDate, ProductionYear, Random)")]
    public string? Sort { get; set; }

    [CommandOption("--desc")]
    [Description("Sort descending instead of ascending")]
    public bool Desc { get; set; }

    [CommandOption("--media-type <TYPE>")]
    [Description("Filter by media type (e.g. Video, Audio, Photo)")]
    public string? MediaType { get; set; }

    [CommandOption("--field <FIELDS>")]
    [Description("Extra fields to return, comma-separated (e.g. Overview,Path,MediaStreams)")]
    public string? Field { get; set; }

    [CommandOption("--type <TYPE>")]
    [Description("Include only item types, comma-separated (e.g. Movie,Series,Episode)")]
    public string? ItemType { get; set; }

    [CommandOption("--recursive")]
    [Description("Search recursively within the parent folder")]
    public bool Recursive { get; set; }
}

public sealed class ItemsListCommand : ApiCommand<ItemsListSettings>
{
    public ItemsListCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ItemsListSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var result = await client.Items.GetAsync(config =>
        {
            var q = config.QueryParameters;
            q.StartIndex = settings.Start;
            q.Limit = settings.Limit ?? 50;
            q.Recursive = settings.Recursive || settings.Search is not null;
            q.SearchTerm = settings.Search;

            if (Guid.TryParse(settings.Parent, out var parentId))
                q.ParentId = parentId;

            if (!string.IsNullOrEmpty(settings.Sort))
                q.SortBy = [settings.Sort];

            if (settings.Desc)
                q.SortOrderAsSortOrder = [SortOrder.Descending];

            if (!string.IsNullOrEmpty(settings.MediaType))
                q.MediaTypes = [settings.MediaType];

            if (!string.IsNullOrEmpty(settings.Field))
                q.Fields = settings.Field.Split(',', StringSplitOptions.TrimEntries);

            if (!string.IsNullOrEmpty(settings.ItemType))
                q.IncludeItemTypes = settings.ItemType.Split(',', StringSplitOptions.TrimEntries);

            q.EnableTotalRecordCount = true;
        });

        var items = result?.Items;
        if (items is null || items.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No items found.[/]");
            return 0;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(new
            {
                totalRecordCount = result!.TotalRecordCount,
                startIndex = result.StartIndex,
                items = items.Select(i => new
                {
                    id = i.Id,
                    name = i.Name,
                    type = i.Type?.ToString(),
                    year = i.ProductionYear,
                    runTimeTicks = i.RunTimeTicks,
                }),
            });
            return 0;
        }

        var table = OutputHelper.CreateTable("Id", "Name", "Type", "Year", "Runtime");
        foreach (var item in items)
        {
            table.AddRow(
                item.Id?.ToString() ?? "",
                Markup.Escape(item.Name ?? "(untitled)"),
                item.Type?.ToString() ?? "",
                item.ProductionYear?.ToString() ?? "",
                FormatRuntime(item.RunTimeTicks));
        }

        OutputHelper.WriteTable(table);
        AnsiConsole.MarkupLine($"[dim]Showing {items.Count} of {result!.TotalRecordCount} items[/]");
        return 0;
    }

    private static string FormatRuntime(long? ticks)
    {
        if (ticks is null or 0)
            return "";
        var ts = TimeSpan.FromTicks(ticks.Value);
        return ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours}h {ts.Minutes:D2}m"
            : $"{ts.Minutes}m {ts.Seconds:D2}s";
    }
}

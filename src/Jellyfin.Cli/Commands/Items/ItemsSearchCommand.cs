using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Items;

public sealed class ItemsSearchSettings : GlobalSettings
{
    [CommandArgument(0, "<TERM>")]
    [Description("Search term")]
    public string Term { get; set; } = string.Empty;

    [CommandOption("--media-type <TYPE>")]
    [Description("Filter by media type (e.g. Video, Audio)")]
    public string? MediaType { get; set; }

    [CommandOption("--type <TYPE>")]
    [Description("Include only item types, comma-separated (e.g. Movie,Series)")]
    public string? ItemType { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Term))
            return ValidationResult.Error("A search term is required.");
        return ValidationResult.Success();
    }
}

public sealed class ItemsSearchCommand : ApiCommand<ItemsSearchSettings>
{
    public ItemsSearchCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ItemsSearchSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var result = await client.Items.GetAsync(config =>
        {
            var q = config.QueryParameters;
            q.SearchTerm = settings.Term;
            q.Recursive = true;
            q.Limit = settings.Limit ?? 25;
            q.StartIndex = settings.Start;
            q.EnableTotalRecordCount = true;

            if (!string.IsNullOrEmpty(settings.MediaType))
                q.MediaTypes = [settings.MediaType];

            if (!string.IsNullOrEmpty(settings.ItemType))
                q.IncludeItemTypes = settings.ItemType.Split(',', StringSplitOptions.TrimEntries);
        });

        var items = result?.Items;
        if (items is null || items.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No results found.[/]");
            return 0;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(new
            {
                totalRecordCount = result!.TotalRecordCount,
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
        AnsiConsole.MarkupLine($"[dim]Showing {items.Count} of {result!.TotalRecordCount} results[/]");
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

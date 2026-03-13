using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Items;

public sealed class ItemsGetSettings : GlobalSettings
{
    [CommandArgument(0, "<ID>")]
    [Description("Item ID")]
    public string Id { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        if (!Guid.TryParse(Id, out _))
            return ValidationResult.Error("A valid item ID (GUID) is required.");
        return ValidationResult.Success();
    }
}

public sealed class ItemsGetCommand : ApiCommand<ItemsGetSettings>
{
    public ItemsGetCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ItemsGetSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var itemId = Guid.Parse(settings.Id);
        var item = await client.Items[itemId].GetAsync();

        if (item is null)
        {
            AnsiConsole.MarkupLine("[red]Item not found.[/]");
            return 1;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(item);
            return 0;
        }

        var table = OutputHelper.CreateTable("Field", "Value");
        table.AddRow("Id", item.Id?.ToString() ?? "");
        table.AddRow("Name", Markup.Escape(item.Name ?? ""));
        table.AddRow("Type", item.Type?.ToString() ?? "");
        table.AddRow("Media Type", item.MediaType?.ToString() ?? "");
        table.AddRow("Year", item.ProductionYear?.ToString() ?? "");
        table.AddRow("Runtime", FormatRuntime(item.RunTimeTicks));
        table.AddRow("Community Rating", item.CommunityRating?.ToString("0.0") ?? "");
        table.AddRow("Official Rating", Markup.Escape(item.OfficialRating ?? ""));
        table.AddRow("Premiere Date", item.PremiereDate?.ToString("yyyy-MM-dd") ?? "");
        table.AddRow("Series", Markup.Escape(item.SeriesName ?? ""));
        table.AddRow("Album", Markup.Escape(item.Album ?? ""));
        table.AddRow("Path", Markup.Escape(item.Path ?? ""));

        if (!string.IsNullOrWhiteSpace(item.Overview))
        {
            var overview = item.Overview.Length > 300
                ? item.Overview[..300] + "..."
                : item.Overview;
            table.AddRow("Overview", Markup.Escape(overview));
        }

        OutputHelper.WriteTable(table);
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

using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Items;

public sealed class ItemsNfoInspectSettings : GlobalSettings
{
    [CommandArgument(0, "<ID>")]
    [Description("Item ID")]
    public string Id { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        return Guid.TryParse(Id, out _)
            ? ValidationResult.Success()
            : ValidationResult.Error("A valid item ID is required.");
    }
}

public sealed class ItemsNfoInspectCommand : ApiCommand<ItemsNfoInspectSettings>
{
    public ItemsNfoInspectCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ItemsNfoInspectSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var itemId = Guid.Parse(settings.Id);
        var userId = await ResolveOptionalUserIdAsync(settings, client, cancellationToken);
        var item = await ItemDiagnosticHelpers.GetDiagnosticItemAsync(client, itemId, userId, cancellationToken);

        if (item is null)
        {
            AnsiConsole.MarkupLine("[red]Item not found.[/]");
            return 1;
        }

        var inspection = ItemDiagnosticHelpers.InspectNfo(item);

        if (settings.Json)
        {
            OutputHelper.WriteJson(new
            {
                id = item.Id,
                name = item.Name,
                type = item.Type?.ToString(),
                path = item.Path,
                localMetadataExists = inspection.MatchedPath is not null,
                pathAccessible = inspection.PathAccessible,
                candidatePaths = inspection.CandidatePaths,
                matchedPath = inspection.MatchedPath,
                parsedDateAdded = inspection.DateAdded,
                rawDateAdded = inspection.DateAddedText,
                likelyOverridesItemDates = inspection.LikelyOverridesItemDates,
                itemDateCreated = item.DateCreated,
                itemDateLastMediaAdded = item.DateLastMediaAdded,
                parseError = inspection.ParseError,
            });
            return 0;
        }

        var table = OutputHelper.CreateTable("Field", "Value");
        table.AddRow("Id", item.Id?.ToString() ?? string.Empty);
        table.AddRow("Name", Markup.Escape(item.Name ?? "(untitled)"));
        table.AddRow("Type", item.Type?.ToString() ?? string.Empty);
        table.AddRow("Item Path", Markup.Escape(item.Path ?? string.Empty));
        table.AddRow("Filesystem Path Accessible", inspection.PathAccessible.ToString());
        table.AddRow("Local NFO Exists", (inspection.MatchedPath is not null).ToString());
        table.AddRow("Matched NFO", Markup.Escape(inspection.MatchedPath ?? string.Empty));
        table.AddRow("Parsed dateadded", ItemDiagnosticHelpers.FormatDate(inspection.DateAdded));
        table.AddRow("Raw dateadded", Markup.Escape(inspection.DateAddedText ?? string.Empty));
        table.AddRow("Likely Overrode Item Dates", inspection.LikelyOverridesItemDates?.ToString() ?? "Unknown");
        table.AddRow("Item DateCreated", ItemDiagnosticHelpers.FormatDate(item.DateCreated));
        table.AddRow("Item DateLastMediaAdded", ItemDiagnosticHelpers.FormatDate(item.DateLastMediaAdded));
        table.AddRow("Parse Error", Markup.Escape(inspection.ParseError ?? string.Empty));

        OutputHelper.WriteTable(table);

        if (inspection.CandidatePaths.Count > 0)
        {
            AnsiConsole.MarkupLine("[dim]Candidate NFO paths:[/]");
            foreach (var candidate in inspection.CandidatePaths)
                AnsiConsole.MarkupLine($"[dim]- {Markup.Escape(candidate)}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[dim]No candidate NFO path could be derived from the item's filesystem path.[/]");
        }

        if (!inspection.PathAccessible)
            AnsiConsole.MarkupLine("[yellow]The CLI host cannot access the item's filesystem path, so NFO inspection is best-effort only.[/]");

        return 0;
    }
}

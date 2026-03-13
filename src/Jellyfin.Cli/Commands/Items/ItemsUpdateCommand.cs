using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Api.Generated.Models;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Items;

public sealed class ItemsUpdateSettings : GlobalSettings
{
    [CommandArgument(0, "<ID>")]
    [Description("Item ID to update")]
    public string Id { get; set; } = string.Empty;

    [CommandOption("--name <NAME>")]
    [Description("New name for the item")]
    public string? Name { get; set; }

    [CommandOption("--overview <TEXT>")]
    [Description("New overview / description")]
    public string? Overview { get; set; }

    [CommandOption("--year <YEAR>")]
    [Description("Production year")]
    public int? Year { get; set; }

    [CommandOption("--rating <RATING>")]
    [Description("Official rating (e.g. PG-13, TV-MA)")]
    public string? Rating { get; set; }

    [CommandOption("--community-rating <VALUE>")]
    [Description("Community rating (0-10)")]
    public double? CommunityRating { get; set; }

    public override ValidationResult Validate()
    {
        if (!Guid.TryParse(Id, out _))
            return ValidationResult.Error("A valid item ID (GUID) is required.");

        if (Name is null && Overview is null && Year is null
            && Rating is null && CommunityRating is null)
        {
            return ValidationResult.Error(
                "At least one field to update is required (--name, --overview, --year, --rating, --community-rating).");
        }

        return ValidationResult.Success();
    }
}

public sealed class ItemsUpdateCommand : ApiCommand<ItemsUpdateSettings>
{
    public ItemsUpdateCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ItemsUpdateSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var itemId = Guid.Parse(settings.Id);

        // Fetch the current item so we can merge changes.
        var existing = await client.Items[itemId].GetAsync();
        if (existing is null)
        {
            AnsiConsole.MarkupLine("[red]Item not found.[/]");
            return 1;
        }

        if (settings.Name is not null)
            existing.Name = settings.Name;

        if (settings.Overview is not null)
            existing.Overview = settings.Overview;

        if (settings.Year is not null)
            existing.ProductionYear = settings.Year;

        if (settings.Rating is not null)
            existing.OfficialRating = settings.Rating;

        if (settings.CommunityRating is not null)
            existing.CommunityRating = (float)settings.CommunityRating.Value;

        await client.Items[itemId].PostAsync(existing);

        AnsiConsole.MarkupLine($"[green]Item {settings.Id} updated successfully.[/]");

        if (settings.Verbose)
        {
            var table = OutputHelper.CreateTable("Field", "New Value");
            if (settings.Name is not null)
                table.AddRow("Name", Markup.Escape(settings.Name));
            if (settings.Overview is not null)
                table.AddRow("Overview", Markup.Escape(Truncate(settings.Overview, 100)));
            if (settings.Year is not null)
                table.AddRow("Year", settings.Year.Value.ToString());
            if (settings.Rating is not null)
                table.AddRow("Rating", Markup.Escape(settings.Rating));
            if (settings.CommunityRating is not null)
                table.AddRow("Community Rating", settings.CommunityRating.Value.ToString("0.0"));
            OutputHelper.WriteTable(table);
        }

        return 0;
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "...";
}

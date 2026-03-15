using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Items;

public sealed class ItemsSubtitlesSearchSettings : GlobalSettings
{
    [CommandArgument(0, "<ITEM_ID>")]
    [Description("Item ID")]
    public string ItemId { get; set; } = string.Empty;

    [CommandArgument(1, "<LANGUAGE>")]
    [Description("Subtitle language or language-id path segment")]
    public string Language { get; set; } = string.Empty;

    [CommandOption("--perfect-match")]
    [Description("Only return perfect matches")]
    public bool PerfectMatchOnly { get; set; }

    public override ValidationResult Validate()
    {
        if (!Guid.TryParse(ItemId, out _))
            return ValidationResult.Error("A valid item ID (GUID) is required.");

        return string.IsNullOrWhiteSpace(Language)
            ? ValidationResult.Error("A subtitle language is required.")
            : ValidationResult.Success();
    }
}

public sealed class ItemsSubtitlesSearchCommand : ApiCommand<ItemsSubtitlesSearchSettings>
{
    public ItemsSubtitlesSearchCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ItemsSubtitlesSearchSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var subtitles = await client.Items[Guid.Parse(settings.ItemId)].RemoteSearch.Subtitles[settings.Language].GetAsync(config =>
        {
            config.QueryParameters.IsPerfectMatch = settings.PerfectMatchOnly ? true : null;
        }, cancellationToken);

        if (subtitles is null || subtitles.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No remote subtitles found.[/]");
            return 0;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(subtitles);
            return 0;
        }

        var table = OutputHelper.CreateTable("Id", "Name", "Provider", "Language", "Format", "Rating", "Downloads");
        foreach (var subtitle in subtitles)
        {
            table.AddRow(
                subtitle.Id ?? string.Empty,
                Markup.Escape(subtitle.Name ?? string.Empty),
                Markup.Escape(subtitle.ProviderName ?? string.Empty),
                Markup.Escape(subtitle.ThreeLetterISOLanguageName ?? string.Empty),
                Markup.Escape(subtitle.Format ?? string.Empty),
                subtitle.CommunityRating?.ToString("0.0") ?? string.Empty,
                subtitle.DownloadCount?.ToString() ?? string.Empty);
        }

        OutputHelper.WriteTable(table);
        return 0;
    }
}

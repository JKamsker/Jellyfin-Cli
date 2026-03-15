using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Items;

public sealed class ItemsLyricsSearchSettings : GlobalSettings
{
    [CommandArgument(0, "<ITEM_ID>")]
    [Description("Audio item ID")]
    public string ItemId { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        return Guid.TryParse(ItemId, out _)
            ? ValidationResult.Success()
            : ValidationResult.Error("A valid audio item ID (GUID) is required.");
    }
}

public sealed class ItemsLyricsSearchCommand : ApiCommand<ItemsLyricsSearchSettings>
{
    public ItemsLyricsSearchCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ItemsLyricsSearchSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var lyrics = await client.Audio[Guid.Parse(settings.ItemId)].RemoteSearch.Lyrics.GetAsync(cancellationToken: cancellationToken);

        if (lyrics is null || lyrics.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No remote lyrics found.[/]");
            return 0;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(lyrics);
            return 0;
        }

        var table = OutputHelper.CreateTable("Id", "Provider", "Title", "Artist", "Album", "Synced", "Lines");
        foreach (var lyric in lyrics)
        {
            table.AddRow(
                lyric.Id ?? string.Empty,
                Markup.Escape(lyric.ProviderName ?? string.Empty),
                Markup.Escape(lyric.Lyrics?.Metadata?.Title ?? string.Empty),
                Markup.Escape(lyric.Lyrics?.Metadata?.Artist ?? string.Empty),
                Markup.Escape(lyric.Lyrics?.Metadata?.Album ?? string.Empty),
                lyric.Lyrics?.Metadata?.IsSynced is true ? "Yes" : "No",
                lyric.Lyrics?.Lyrics?.Count.ToString() ?? string.Empty);
        }

        OutputHelper.WriteTable(table);
        return 0;
    }
}

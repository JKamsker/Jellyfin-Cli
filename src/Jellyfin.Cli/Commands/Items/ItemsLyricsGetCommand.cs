using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Items;

public sealed class ItemsLyricsGetSettings : GlobalSettings
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

public sealed class ItemsLyricsGetCommand : ApiCommand<ItemsLyricsGetSettings>
{
    public ItemsLyricsGetCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ItemsLyricsGetSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var lyric = await client.Audio[Guid.Parse(settings.ItemId)].Lyrics.GetAsync(cancellationToken: cancellationToken);
        return ItemsCommandOutputHelper.WriteLyrics(settings, lyric, "No lyrics found.");
    }
}

using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Items;

public sealed class ItemsLyricsDownloadSettings : GlobalSettings
{
    [CommandArgument(0, "<ITEM_ID>")]
    [Description("Audio item ID")]
    public string ItemId { get; set; } = string.Empty;

    [CommandArgument(1, "<LYRIC_ID>")]
    [Description("Remote lyric ID")]
    public string LyricId { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        if (!Guid.TryParse(ItemId, out _))
            return ValidationResult.Error("A valid audio item ID (GUID) is required.");

        return string.IsNullOrWhiteSpace(LyricId)
            ? ValidationResult.Error("A lyric ID is required.")
            : ValidationResult.Success();
    }
}

public sealed class ItemsLyricsDownloadCommand : ApiCommand<ItemsLyricsDownloadSettings>
{
    public ItemsLyricsDownloadCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ItemsLyricsDownloadSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var lyric = await client.Audio[Guid.Parse(settings.ItemId)].RemoteSearch.Lyrics[settings.LyricId].PostAsync(cancellationToken: cancellationToken);
        if (settings.Json)
        {
            OutputHelper.WriteJson(lyric);
            return 0;
        }

        AnsiConsole.MarkupLine(
            $"[green]Downloaded remote lyric [white]{Markup.Escape(settings.LyricId)}[/] for item [white]{settings.ItemId}[/].[/]");
        return 0;
    }
}

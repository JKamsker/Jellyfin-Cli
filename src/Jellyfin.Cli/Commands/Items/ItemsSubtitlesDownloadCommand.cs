using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Items;

public sealed class ItemsSubtitlesDownloadSettings : GlobalSettings
{
    [CommandArgument(0, "<ITEM_ID>")]
    [Description("Item ID")]
    public string ItemId { get; set; } = string.Empty;

    [CommandArgument(1, "<SUBTITLE_ID>")]
    [Description("Remote subtitle ID or language-id path segment")]
    public string SubtitleId { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        if (!Guid.TryParse(ItemId, out _))
            return ValidationResult.Error("A valid item ID (GUID) is required.");

        return string.IsNullOrWhiteSpace(SubtitleId)
            ? ValidationResult.Error("A subtitle ID is required.")
            : ValidationResult.Success();
    }
}

public sealed class ItemsSubtitlesDownloadCommand : ApiCommand<ItemsSubtitlesDownloadSettings>
{
    public ItemsSubtitlesDownloadCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ItemsSubtitlesDownloadSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        await client.Items[Guid.Parse(settings.ItemId)].RemoteSearch.Subtitles[settings.SubtitleId].PostAsync(cancellationToken: cancellationToken);
        AnsiConsole.MarkupLine(
            $"[green]Downloaded remote subtitle [white]{Markup.Escape(settings.SubtitleId)}[/] for item [white]{settings.ItemId}[/].[/]");
        return 0;
    }
}

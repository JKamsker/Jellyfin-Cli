using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Items;

public sealed class ItemsLyricsUploadSettings : GlobalSettings
{
    [CommandArgument(0, "<ITEM_ID>")]
    [Description("Audio item ID")]
    public string ItemId { get; set; } = string.Empty;

    [CommandArgument(1, "<FILE>")]
    [Description("Lyric file path")]
    public string FilePath { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        if (!Guid.TryParse(ItemId, out _))
            return ValidationResult.Error("A valid audio item ID (GUID) is required.");

        return File.Exists(FilePath)
            ? ValidationResult.Success()
            : ValidationResult.Error($"Lyric file not found: {FilePath}");
    }
}

public sealed class ItemsLyricsUploadCommand : ApiCommand<ItemsLyricsUploadSettings>
{
    public ItemsLyricsUploadCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ItemsLyricsUploadSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(settings.FilePath);
        var lyric = await client.Audio[Guid.Parse(settings.ItemId)].Lyrics.PostAsync(stream, config =>
        {
            config.QueryParameters.FileName = Path.GetFileName(settings.FilePath);
        }, cancellationToken);

        if (settings.Json)
        {
            OutputHelper.WriteJson(lyric);
            return 0;
        }

        AnsiConsole.MarkupLine($"[green]Uploaded lyric file [white]{Markup.Escape(settings.FilePath)}[/].[/]");
        return 0;
    }
}

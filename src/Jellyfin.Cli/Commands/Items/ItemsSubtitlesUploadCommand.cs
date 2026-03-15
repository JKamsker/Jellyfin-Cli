using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Api.Generated.Models;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Items;

public sealed class ItemsSubtitlesUploadSettings : GlobalSettings
{
    [CommandArgument(0, "<ITEM_ID>")]
    [Description("Item ID")]
    public string ItemId { get; set; } = string.Empty;

    [CommandArgument(1, "<FILE>")]
    [Description("Subtitle file path")]
    public string FilePath { get; set; } = string.Empty;

    [CommandOption("--language <CODE>")]
    [Description("Subtitle language code")]
    public string? Language { get; set; }

    [CommandOption("--format <FORMAT>")]
    [Description("Subtitle format, defaults to the file extension")]
    public string? Format { get; set; }

    [CommandOption("--forced")]
    [Description("Mark the subtitle as forced")]
    public bool IsForced { get; set; }

    [CommandOption("--hearing-impaired")]
    [Description("Mark the subtitle as hearing impaired")]
    public bool IsHearingImpaired { get; set; }

    public override ValidationResult Validate()
    {
        if (!Guid.TryParse(ItemId, out _))
            return ValidationResult.Error("A valid item ID (GUID) is required.");

        return File.Exists(FilePath)
            ? ValidationResult.Success()
            : ValidationResult.Error($"Subtitle file not found: {FilePath}");
    }
}

public sealed class ItemsSubtitlesUploadCommand : ApiCommand<ItemsSubtitlesUploadSettings>
{
    public ItemsSubtitlesUploadCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ItemsSubtitlesUploadSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var format = !string.IsNullOrWhiteSpace(settings.Format)
            ? settings.Format
            : Path.GetExtension(settings.FilePath).TrimStart('.');

        if (string.IsNullOrWhiteSpace(format))
        {
            AnsiConsole.MarkupLine("[red]Unable to infer subtitle format. Specify --format.[/]");
            return 1;
        }

        var body = new UploadSubtitleDto
        {
            Data = await File.ReadAllTextAsync(settings.FilePath, cancellationToken),
            Format = format,
            Language = settings.Language,
            IsForced = settings.IsForced,
            IsHearingImpaired = settings.IsHearingImpaired,
        };

        await client.Videos[Guid.Parse(settings.ItemId)].Subtitles.PostAsync(body, cancellationToken: cancellationToken);
        AnsiConsole.MarkupLine($"[green]Uploaded subtitle file [white]{Markup.Escape(settings.FilePath)}[/].[/]");
        return 0;
    }
}

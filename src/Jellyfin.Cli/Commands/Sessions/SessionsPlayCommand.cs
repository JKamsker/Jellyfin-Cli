using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Api.Generated.Sessions.Item.Playing;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Sessions;

public sealed class SessionsPlaySettings : GlobalSettings
{
    [CommandArgument(0, "<SESSION_ID>")]
    [Description("Target session id")]
    public string SessionId { get; set; } = string.Empty;

    [CommandArgument(1, "<ITEM_IDS>")]
    [Description("Comma-separated item ids to play")]
    public string ItemIds { get; set; } = string.Empty;

    [CommandOption("--play-command <COMMAND>")]
    [Description("Play command: PlayNow (default), PlayNext, PlayLast, PlayInstantMix, PlayShuffle")]
    public string? PlayCommandName { get; set; }

    [CommandOption("--start-position <TICKS>")]
    [Description("Starting position in ticks")]
    public long? StartPositionTicks { get; set; }

    [CommandOption("--audio-stream <INDEX>")]
    [Description("Audio stream index")]
    public int? AudioStreamIndex { get; set; }

    [CommandOption("--subtitle-stream <INDEX>")]
    [Description("Subtitle stream index")]
    public int? SubtitleStreamIndex { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(SessionId))
            return ValidationResult.Error("SESSION_ID is required.");

        if (string.IsNullOrWhiteSpace(ItemIds))
            return ValidationResult.Error("ITEM_IDS is required.");

        return ValidationResult.Success();
    }
}

public sealed class SessionsPlayCommand : ApiCommand<SessionsPlaySettings>
{
    public SessionsPlayCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, SessionsPlaySettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var itemGuids = ParseItemIds(settings.ItemIds);
        if (itemGuids is null)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] ITEM_IDS must be a comma-separated list of valid GUIDs.");
            return 1;
        }

        var playCommand = ResolvePlayCommand(settings.PlayCommandName);

        await client.Sessions[settings.SessionId].Playing.PostAsync(config =>
        {
            config.QueryParameters.ItemIds = itemGuids;
            config.QueryParameters.PlayCommandAsPlayCommand = playCommand;
            config.QueryParameters.StartPositionTicks = settings.StartPositionTicks;
            config.QueryParameters.AudioStreamIndex = settings.AudioStreamIndex;
            config.QueryParameters.SubtitleStreamIndex = settings.SubtitleStreamIndex;
        });

        AnsiConsole.MarkupLine("[green]Play command sent.[/]");
        return 0;
    }

    private static Guid?[]? ParseItemIds(string raw)
    {
        var parts = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var guids = new Guid?[parts.Length];
        for (var i = 0; i < parts.Length; i++)
        {
            if (!Guid.TryParse(parts[i], out var g))
                return null;
            guids[i] = g;
        }
        return guids;
    }

    private static PlayCommand ResolvePlayCommand(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return PlayCommand.PlayNow;

        return name.ToLowerInvariant() switch
        {
            "playnow" => PlayCommand.PlayNow,
            "playnext" => PlayCommand.PlayNext,
            "playlast" => PlayCommand.PlayLast,
            "playinstantmix" => PlayCommand.PlayInstantMix,
            "playshuffle" => PlayCommand.PlayShuffle,
            _ => PlayCommand.PlayNow,
        };
    }
}

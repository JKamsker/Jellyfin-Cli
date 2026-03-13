using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Sessions;

public sealed class SessionsStateSettings : GlobalSettings
{
    [CommandArgument(0, "<ACTION>")]
    [Description("Playstate action: pause, resume, stop, next, prev, seek, rewind, ff, toggle")]
    public string Action { get; set; } = string.Empty;

    [CommandArgument(1, "<SESSION_ID>")]
    [Description("Target session id")]
    public string SessionId { get; set; } = string.Empty;

    [CommandOption("--position <TICKS>")]
    [Description("Seek position in ticks (required for seek)")]
    public long? Position { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Action))
            return ValidationResult.Error("ACTION is required.");

        if (string.IsNullOrWhiteSpace(SessionId))
            return ValidationResult.Error("SESSION_ID is required.");

        var action = Action.ToLowerInvariant();
        var valid = new[] { "pause", "resume", "stop", "next", "prev", "seek", "rewind", "ff", "toggle" };
        if (!valid.Contains(action))
            return ValidationResult.Error($"ACTION must be one of: {string.Join(", ", valid)}");

        if (action == "seek" && Position is null)
            return ValidationResult.Error("--position is required for seek.");

        return ValidationResult.Success();
    }
}

public sealed class SessionsStateCommand : ApiCommand<SessionsStateSettings>
{
    public SessionsStateCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, SessionsStateSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        // The API path is /Sessions/{sessionId}/Playing/{command}
        // where {command} is the PlaystateCommand enum value:
        //   Stop, Pause, Unpause, NextTrack, PreviousTrack, Seek, Rewind, FastForward, PlayPause
        var command = MapActionToCommand(settings.Action);

        await client.Sessions[settings.SessionId].Playing[command].PostAsync(config =>
        {
            config.QueryParameters.SeekPositionTicks = settings.Position;
        });

        AnsiConsole.MarkupLine($"[green]Playstate command '{command}' sent.[/]");
        return 0;
    }

    private static string MapActionToCommand(string action)
    {
        return action.ToLowerInvariant() switch
        {
            "pause" => "Pause",
            "resume" => "Unpause",
            "stop" => "Stop",
            "next" => "NextTrack",
            "prev" => "PreviousTrack",
            "seek" => "Seek",
            "rewind" => "Rewind",
            "ff" => "FastForward",
            "toggle" => "PlayPause",
            _ => action,
        };
    }
}

using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Api.Generated.Models;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Playlists;

// ---------------------------------------------------------------------------
// Settings
// ---------------------------------------------------------------------------

public sealed class PlaylistsUpdateSettings : GlobalSettings
{
    [CommandArgument(0, "<PLAYLIST_ID>")]
    [Description("The playlist ID")]
    public string PlaylistId { get; set; } = string.Empty;

    [CommandOption("--name <NAME>")]
    [Description("New name for the playlist")]
    public string? Name { get; set; }

    [CommandOption("--public")]
    [Description("Make the playlist publicly visible")]
    public bool? IsPublic { get; set; }

    public override ValidationResult Validate()
    {
        if (!Guid.TryParse(PlaylistId, out _))
            return ValidationResult.Error("PLAYLIST_ID must be a valid GUID.");

        if (string.IsNullOrWhiteSpace(Name) && IsPublic is null)
            return ValidationResult.Error("At least one of --name or --public must be specified.");

        return ValidationResult.Success();
    }
}

// ---------------------------------------------------------------------------
// Command
// ---------------------------------------------------------------------------

public sealed class PlaylistsUpdateCommand : ApiCommand<PlaylistsUpdateSettings>
{
    public PlaylistsUpdateCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, PlaylistsUpdateSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var playlistId = Guid.Parse(settings.PlaylistId);

        var dto = new UpdatePlaylistDto
        {
            Name = settings.Name,
            IsPublic = settings.IsPublic,
        };

        await client.Playlists[playlistId].PostAsync(dto);

        AnsiConsole.MarkupLine($"[green]Playlist '{settings.PlaylistId}' updated.[/]");
        return 0;
    }
}

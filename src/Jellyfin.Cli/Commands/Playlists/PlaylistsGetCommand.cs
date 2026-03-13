using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Playlists;

// ---------------------------------------------------------------------------
// Settings
// ---------------------------------------------------------------------------

public sealed class PlaylistsGetSettings : GlobalSettings
{
    [CommandArgument(0, "<PLAYLIST_ID>")]
    [Description("The playlist ID")]
    public string PlaylistId { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        if (!Guid.TryParse(PlaylistId, out _))
            return ValidationResult.Error("PLAYLIST_ID must be a valid GUID.");

        return ValidationResult.Success();
    }
}

// ---------------------------------------------------------------------------
// Command
// ---------------------------------------------------------------------------

public sealed class PlaylistsGetCommand : ApiCommand<PlaylistsGetSettings>
{
    public PlaylistsGetCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, PlaylistsGetSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var playlistId = Guid.Parse(settings.PlaylistId);
        var playlist = await client.Playlists[playlistId].GetAsync();

        if (playlist is null)
        {
            AnsiConsole.MarkupLine("[red]Playlist not found.[/]");
            return 1;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(new
            {
                id = settings.PlaylistId,
                openAccess = playlist.OpenAccess,
                itemIds = playlist.ItemIds?.Select(id => id?.ToString()),
                shares = playlist.Shares?.Select(s => new
                {
                    userId = s.UserId,
                    canEdit = s.CanEdit,
                }),
            });
            return 0;
        }

        var table = OutputHelper.CreateTable("Field", "Value");
        table.AddRow("ID", settings.PlaylistId);
        table.AddRow("Open Access", playlist.OpenAccess?.ToString() ?? "(unknown)");
        table.AddRow("Item Count", playlist.ItemIds?.Count.ToString() ?? "0");

        if (playlist.ItemIds is { Count: > 0 })
        {
            table.AddRow("Item IDs", string.Join(", ", playlist.ItemIds));
        }

        OutputHelper.WriteTable(table);
        return 0;
    }
}

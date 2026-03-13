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

public sealed class PlaylistsCreateSettings : GlobalSettings
{
    [CommandOption("--name <NAME>")]
    [Description("Name for the new playlist")]
    public string Name { get; set; } = string.Empty;

    [CommandOption("--items <IDS>")]
    [Description("Comma-separated item IDs to add to the playlist")]
    public string? Items { get; set; }

    [CommandOption("--public")]
    [Description("Make the playlist publicly visible")]
    public bool? IsPublic { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return ValidationResult.Error("--name is required.");

        if (!string.IsNullOrWhiteSpace(Items))
        {
            foreach (var part in Items.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!Guid.TryParse(part, out _))
                    return ValidationResult.Error($"Invalid item ID: '{part}'.");
            }
        }

        return ValidationResult.Success();
    }
}

// ---------------------------------------------------------------------------
// Command
// ---------------------------------------------------------------------------

public sealed class PlaylistsCreateCommand : ApiCommand<PlaylistsCreateSettings>
{
    public PlaylistsCreateCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, PlaylistsCreateSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var dto = new CreatePlaylistDto
        {
            Name = settings.Name,
            IsPublic = settings.IsPublic,
        };

        if (!string.IsNullOrWhiteSpace(settings.User) && Guid.TryParse(settings.User, out var userId))
        {
            dto.UserId = userId;
        }

        if (!string.IsNullOrWhiteSpace(settings.Items))
        {
            dto.Ids = settings.Items
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => (Guid?)Guid.Parse(s))
                .ToList();
        }

        var result = await client.Playlists.PostAsync(dto);

        if (result is null)
        {
            AnsiConsole.MarkupLine("[red]Failed to create playlist.[/]");
            return 1;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(new { id = result.Id, name = settings.Name });
            return 0;
        }

        AnsiConsole.MarkupLine($"[green]Playlist '[white]{settings.Name}[/]' created (ID: {result.Id}).[/]");
        return 0;
    }
}

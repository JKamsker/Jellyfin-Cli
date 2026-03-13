using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Microsoft.Kiota.Abstractions;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Playlists;

// ===========================================================================
// List items in a playlist
// ===========================================================================

public sealed class PlaylistItemsListSettings : GlobalSettings
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

public sealed class PlaylistItemsListCommand : ApiCommand<PlaylistItemsListSettings>
{
    public PlaylistItemsListCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, PlaylistItemsListSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var playlistId = Guid.Parse(settings.PlaylistId);

        Guid? userId = null;
        if (!string.IsNullOrWhiteSpace(settings.User) && Guid.TryParse(settings.User, out var uid))
        {
            userId = uid;
        }

        var result = await client.Playlists[playlistId].Items.GetAsync(cfg =>
        {
            cfg.QueryParameters.Limit = settings.Limit;
            cfg.QueryParameters.StartIndex = settings.Start;
            cfg.QueryParameters.UserId = userId;
        });

        var items = result?.Items;
        if (items is null || items.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No items in this playlist.[/]");
            return 0;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(new
            {
                totalRecordCount = result!.TotalRecordCount,
                items = items.Select(i => new
                {
                    id = i.Id,
                    playlistItemId = i.PlaylistItemId,
                    name = i.Name,
                    type = i.Type?.ToString(),
                    runTimeTicks = i.RunTimeTicks,
                }),
            });
            return 0;
        }

        AnsiConsole.MarkupLine($"[dim]Total items: {result!.TotalRecordCount}[/]");
        var table = OutputHelper.CreateTable("#", "ID", "Playlist Item ID", "Name", "Type");

        for (var idx = 0; idx < items.Count; idx++)
        {
            var item = items[idx];
            table.AddRow(
                (idx + 1 + (settings.Start ?? 0)).ToString(),
                item.Id?.ToString() ?? "",
                item.PlaylistItemId ?? "",
                item.Name ?? "(untitled)",
                item.Type?.ToString() ?? "");
        }

        OutputHelper.WriteTable(table);
        return 0;
    }
}

// ===========================================================================
// Add items to a playlist
// ===========================================================================

public sealed class PlaylistItemsAddSettings : GlobalSettings
{
    [CommandArgument(0, "<PLAYLIST_ID>")]
    [Description("The playlist ID")]
    public string PlaylistId { get; set; } = string.Empty;

    [CommandOption("--items <IDS>")]
    [Description("Comma-separated item IDs to add")]
    public string Items { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        if (!Guid.TryParse(PlaylistId, out _))
            return ValidationResult.Error("PLAYLIST_ID must be a valid GUID.");

        if (string.IsNullOrWhiteSpace(Items))
            return ValidationResult.Error("--items is required.");

        foreach (var part in Items.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!Guid.TryParse(part, out _))
                return ValidationResult.Error($"Invalid item ID: '{part}'.");
        }

        return ValidationResult.Success();
    }
}

public sealed class PlaylistItemsAddCommand : ApiCommand<PlaylistItemsAddSettings>
{
    public PlaylistItemsAddCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, PlaylistItemsAddSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var playlistId = Guid.Parse(settings.PlaylistId);
        var ids = settings.Items
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => (Guid?)Guid.Parse(s))
            .ToArray();

        Guid? userId = null;
        if (!string.IsNullOrWhiteSpace(settings.User) && Guid.TryParse(settings.User, out var uid))
        {
            userId = uid;
        }

        await client.Playlists[playlistId].Items.PostAsync(cfg =>
        {
            cfg.QueryParameters.Ids = ids;
            cfg.QueryParameters.UserId = userId;
        });

        AnsiConsole.MarkupLine($"[green]Added {ids.Length} item(s) to playlist '{settings.PlaylistId}'.[/]");
        return 0;
    }
}

// ===========================================================================
// Remove items from a playlist
// ===========================================================================

public sealed class PlaylistItemsRemoveSettings : GlobalSettings
{
    [CommandArgument(0, "<PLAYLIST_ID>")]
    [Description("The playlist ID")]
    public string PlaylistId { get; set; } = string.Empty;

    [CommandOption("--entry-ids <IDS>")]
    [Description("Comma-separated playlist entry IDs to remove")]
    public string EntryIds { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        if (!Guid.TryParse(PlaylistId, out _))
            return ValidationResult.Error("PLAYLIST_ID must be a valid GUID.");

        if (string.IsNullOrWhiteSpace(EntryIds))
            return ValidationResult.Error("--entry-ids is required.");

        return ValidationResult.Success();
    }
}

public sealed class PlaylistItemsRemoveCommand : ApiCommand<PlaylistItemsRemoveSettings>
{
    public PlaylistItemsRemoveCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, PlaylistItemsRemoveSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var playlistId = Guid.Parse(settings.PlaylistId);
        var entryIds = settings.EntryIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (!settings.Yes)
        {
            if (!OutputHelper.Confirm($"Remove {entryIds.Length} item(s) from playlist '{settings.PlaylistId}'?"))
            {
                AnsiConsole.MarkupLine("[yellow]Cancelled.[/]");
                return 0;
            }
        }

        await client.Playlists[playlistId].Items.DeleteAsync(cfg =>
        {
            cfg.QueryParameters.EntryIds = entryIds;
        });

        AnsiConsole.MarkupLine($"[green]Removed {entryIds.Length} item(s) from playlist '{settings.PlaylistId}'.[/]");
        return 0;
    }
}

// ===========================================================================
// Move an item within a playlist
// ===========================================================================

public sealed class PlaylistItemsMoveSettings : GlobalSettings
{
    [CommandArgument(0, "<PLAYLIST_ID>")]
    [Description("The playlist ID")]
    public string PlaylistId { get; set; } = string.Empty;

    [CommandOption("--item-id <ID>")]
    [Description("The item ID to move")]
    public string ItemId { get; set; } = string.Empty;

    [CommandOption("--new-index <INDEX>")]
    [Description("The new zero-based index for the item")]
    public int NewIndex { get; set; }

    public override ValidationResult Validate()
    {
        if (!Guid.TryParse(PlaylistId, out _))
            return ValidationResult.Error("PLAYLIST_ID must be a valid GUID.");

        if (string.IsNullOrWhiteSpace(ItemId))
            return ValidationResult.Error("--item-id is required.");

        if (NewIndex < 0)
            return ValidationResult.Error("--new-index must be zero or greater.");

        return ValidationResult.Success();
    }
}

public sealed class PlaylistItemsMoveCommand : ApiCommand<PlaylistItemsMoveSettings>
{
    public PlaylistItemsMoveCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, PlaylistItemsMoveSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var playlistId = Guid.Parse(settings.PlaylistId);

        await client.Playlists[playlistId].Items[settings.ItemId].Move[settings.NewIndex].PostAsync();

        AnsiConsole.MarkupLine(
            $"[green]Moved item '{settings.ItemId}' to index {settings.NewIndex} " +
            $"in playlist '{settings.PlaylistId}'.[/]");
        return 0;
    }
}

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Api.Generated.Library.VirtualFolders;
using Jellyfin.Cli.Api.Generated.Models;

namespace Jellyfin.Cli.Commands.Library;

internal static class LibraryCommandHelper
{
    internal static CollectionTypeOptions ParseCollectionType(string value)
    {
        return Enum.TryParse<CollectionTypeOptions>(value, true, out var parsed)
            ? parsed
            : throw new InvalidOperationException(
                $"Unknown collection type '{value}'. Valid values: movies, tvshows, music, musicvideos, homevideos, boxsets, books, mixed.");
    }

    internal static async Task<VirtualFolderInfo> GetFolderByNameAsync(
        JellyfinApiClient client,
        string folderName,
        CancellationToken cancellationToken)
    {
        var folders = await client.Library.VirtualFolders.GetAsync(cancellationToken: cancellationToken) ?? [];
        var folder = folders.FirstOrDefault(folder =>
            string.Equals(folder.Name, folderName, StringComparison.OrdinalIgnoreCase));

        return folder
            ?? throw new InvalidOperationException($"Virtual folder '{folderName}' was not found.");
    }
}

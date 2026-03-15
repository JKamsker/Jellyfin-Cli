using System.Globalization;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Api.Generated.Models;

using Spectre.Console;

namespace Jellyfin.Cli.Commands.Items;

internal sealed record ItemTreeEntry(BaseItemDto Item, int Depth, DateTimeOffset? DateModified);

internal sealed record NfoInspectionResult(
    bool PathAccessible,
    IReadOnlyList<string> CandidatePaths,
    string? MatchedPath,
    DateTimeOffset? DateAdded,
    string? DateAddedText,
    bool? LikelyOverridesItemDates,
    string? ParseError);

internal static class ItemDiagnosticHelpers
{
    internal static readonly ItemFields[] DiagnosticFields =
    [
        ItemFields.DateCreated,
        ItemFields.DateLastMediaAdded,
        ItemFields.ParentId,
        ItemFields.Path,
    ];

    internal static async Task<BaseItemDto?> GetDiagnosticItemAsync(
        JellyfinApiClient client,
        Guid itemId,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        var result = await client.Items.GetAsync(config =>
        {
            var q = config.QueryParameters;
            q.Ids = [itemId];
            q.Limit = 1;
            q.FieldsAsItemFields = DiagnosticFields;
            q.EnableUserData = userId is not null;
            q.UserId = userId;
        }, cancellationToken);

        return result?.Items?.FirstOrDefault();
    }

    internal static async Task<List<BaseItemDto>> GetChildrenAsync(
        JellyfinApiClient client,
        Guid parentId,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        var items = new List<BaseItemDto>();
        var startIndex = 0;
        const int pageSize = 200;

        while (true)
        {
            var page = await client.Items.GetAsync(config =>
            {
                var q = config.QueryParameters;
                q.ParentId = parentId;
                q.Recursive = false;
                q.StartIndex = startIndex;
                q.Limit = pageSize;
                q.FieldsAsItemFields = DiagnosticFields;
                q.EnableUserData = userId is not null;
                q.UserId = userId;
            }, cancellationToken);

            var batch = page?.Items ?? [];
            if (batch.Count == 0)
                break;

            items.AddRange(batch);
            startIndex += batch.Count;

            if (batch.Count < pageSize)
                break;
        }

        return items
            .OrderBy(GetTypeSortOrder)
            .ThenBy(i => i.ParentIndexNumber ?? int.MaxValue)
            .ThenBy(i => i.IndexNumber ?? int.MaxValue)
            .ThenBy(i => i.SortName ?? i.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    internal static async Task<List<ItemTreeEntry>> BuildTreeAsync(
        JellyfinApiClient client,
        HttpClient httpClient,
        BaseItemDto rootItem,
        Guid? userId,
        bool includeDateModified,
        CancellationToken cancellationToken)
    {
        var entries = new List<ItemTreeEntry>();
        await AppendAsync(rootItem, 0, cancellationToken);
        return entries;

        async Task AppendAsync(BaseItemDto item, int depth, CancellationToken ct)
        {
            DateTimeOffset? dateModified = null;
            if (includeDateModified && item.Id is Guid currentId)
            {
                var rawJson = await GetItemJsonAsync(httpClient, currentId, userId, ct);
                dateModified = TryGetDateTimeOffset(rawJson?.RootElement, "DateModified");
            }

            entries.Add(new ItemTreeEntry(item, depth, dateModified));

            if (item.Id is not Guid itemId || item.IsFolder != true)
                return;

            var children = await GetChildrenAsync(client, itemId, userId, ct);
            foreach (var child in children)
                await AppendAsync(child, depth + 1, ct);
        }
    }

    internal static async Task<JsonDocument?> GetItemJsonAsync(
        HttpClient httpClient,
        Guid itemId,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        var relativePath = userId is Guid resolvedUserId
            ? $"Items/{itemId}?userId={Uri.EscapeDataString(resolvedUserId.ToString())}"
            : $"Items/{itemId}";

        using var response = await httpClient.GetAsync(relativePath, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    internal static DateTimeOffset? TryGetDateTimeOffset(JsonElement? element, string propertyName)
    {
        if (element is not JsonElement value || value.ValueKind != JsonValueKind.Object)
            return null;

        if (!value.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
            return null;

        if (!DateTimeOffset.TryParse(property.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
            return null;

        return parsed;
    }

    internal static string? TryGetString(JsonElement? element, string propertyName)
    {
        if (element is not JsonElement value || value.ValueKind != JsonValueKind.Object)
            return null;

        return value.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    internal static string FormatDate(DateTimeOffset? value)
        => value?.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture) ?? string.Empty;

    internal static string FormatName(BaseItemDto item)
        => Markup.Escape(item.Name ?? "(untitled)");

    internal static string FormatNodeLabel(BaseItemDto item, int depth)
    {
        var prefix = depth == 0 ? string.Empty : string.Concat(Enumerable.Repeat("  ", depth - 1)) + "└ ";
        return Markup.Escape(prefix + (item.Name ?? "(untitled)"));
    }

    internal static Guid? GetLatestContainerId(BaseItemDto item)
    {
        return item.Type switch
        {
            BaseItemDto_Type.Episode or BaseItemDto_Type.Season => item.SeriesId ?? item.ParentId ?? item.Id,
            BaseItemDto_Type.Audio => item.AlbumId ?? item.ParentId ?? item.Id,
            _ => item.Id ?? item.ParentId,
        };
    }

    internal static string DescribeLatestContainer(BaseItemDto item)
    {
        return item.Type switch
        {
            BaseItemDto_Type.Episode or BaseItemDto_Type.Season => item.SeriesName ?? item.Name ?? string.Empty,
            BaseItemDto_Type.Audio => item.Album ?? item.Name ?? string.Empty,
            _ => item.Name ?? string.Empty,
        };
    }

    internal static bool IsEligibleForLatest(BaseItemDto item)
    {
        return item.Type switch
        {
            BaseItemDto_Type.Audio => true,
            BaseItemDto_Type.AudioBook => true,
            BaseItemDto_Type.Book => true,
            BaseItemDto_Type.BoxSet => true,
            BaseItemDto_Type.Episode => true,
            BaseItemDto_Type.Movie => true,
            BaseItemDto_Type.MusicAlbum => true,
            BaseItemDto_Type.MusicVideo => true,
            BaseItemDto_Type.Photo => true,
            BaseItemDto_Type.PhotoAlbum => true,
            BaseItemDto_Type.Program => true,
            BaseItemDto_Type.Recording => true,
            BaseItemDto_Type.Season => true,
            BaseItemDto_Type.Series => true,
            BaseItemDto_Type.Trailer => true,
            BaseItemDto_Type.Video => true,
            _ => false,
        };
    }

    internal static NfoInspectionResult InspectNfo(BaseItemDto item)
    {
        var candidates = GetCandidateNfoPaths(item);
        var accessiblePaths = candidates.Where(File.Exists).ToList();

        if (candidates.Count == 0)
            return new NfoInspectionResult(true, candidates, null, null, null, null, "The item does not expose a filesystem path.");

        if (accessiblePaths.Count == 0)
        {
            var parentDirectory = GetParentDirectory(item.Path);
            var pathAccessible = parentDirectory is not null && Directory.Exists(parentDirectory);
            return new NfoInspectionResult(pathAccessible, candidates, null, null, null, null, null);
        }

        foreach (var candidate in accessiblePaths)
        {
            try
            {
                var document = XDocument.Load(candidate, LoadOptions.PreserveWhitespace);
                var dateAddedElement = document
                    .Descendants()
                    .FirstOrDefault(node => string.Equals(node.Name.LocalName, "dateadded", StringComparison.OrdinalIgnoreCase));

                var rawValue = dateAddedElement?.Value?.Trim();
                var parsedValue = ParseDateAdded(rawValue);
                var likelyOverrides = parsedValue is not null && MatchesItemDate(parsedValue.Value, item.DateCreated, item.DateLastMediaAdded);

                return new NfoInspectionResult(true, candidates, candidate, parsedValue, rawValue, likelyOverrides, null);
            }
            catch (Exception ex) when (ex is XmlException or IOException or UnauthorizedAccessException)
            {
                return new NfoInspectionResult(true, candidates, candidate, null, null, null, ex.Message);
            }
        }

        return new NfoInspectionResult(true, candidates, null, null, null, null, null);
    }

    internal static List<string> GetCandidateNfoPaths(BaseItemDto item)
    {
        var path = item.Path;
        if (string.IsNullOrWhiteSpace(path))
            return [];

        var candidates = new List<string>();
        var isDirectory = item.IsFolder == true || Directory.Exists(path);
        var directory = isDirectory ? path : Path.GetDirectoryName(path);
        var fileNameWithoutExtension = isDirectory ? new DirectoryInfo(path).Name : Path.GetFileNameWithoutExtension(path);

        if (!string.IsNullOrWhiteSpace(directory) && !string.IsNullOrWhiteSpace(fileNameWithoutExtension))
            candidates.Add(Path.Combine(directory, $"{fileNameWithoutExtension}.nfo"));

        if (!string.IsNullOrWhiteSpace(directory))
        {
            if (item.Type == BaseItemDto_Type.Series)
            {
                candidates.Add(Path.Combine(directory, "tvshow.nfo"));
            }
            else if (item.Type == BaseItemDto_Type.Season)
            {
                candidates.Add(Path.Combine(directory, "season.nfo"));
                if (item.IndexNumber is int seasonNumber)
                    candidates.Add(Path.Combine(directory, $"season{seasonNumber:D2}.nfo"));
            }
        }

        return candidates
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static int GetTypeSortOrder(BaseItemDto item)
    {
        return item.Type switch
        {
            BaseItemDto_Type.Series => 0,
            BaseItemDto_Type.Season => 1,
            BaseItemDto_Type.Episode => 2,
            _ => 10,
        };
    }

    private static string? GetParentDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        return Directory.Exists(path) ? path : Path.GetDirectoryName(path);
    }

    private static DateTimeOffset? ParseDateAdded(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return null;

        if (DateTimeOffset.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
            return parsed;

        if (DateTimeOffset.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out parsed))
            return parsed;

        return null;
    }

    private static bool MatchesItemDate(DateTimeOffset parsedValue, DateTimeOffset? dateCreated, DateTimeOffset? dateLastMediaAdded)
    {
        return Matches(parsedValue, dateCreated) || Matches(parsedValue, dateLastMediaAdded);
    }

    private static bool Matches(DateTimeOffset left, DateTimeOffset? right)
    {
        return right is DateTimeOffset value && Math.Abs((left - value).TotalSeconds) < 1;
    }
}

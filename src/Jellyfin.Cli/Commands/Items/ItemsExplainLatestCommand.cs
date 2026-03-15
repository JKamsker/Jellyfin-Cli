using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Api.Generated.Models;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Items;

public sealed class ItemsExplainLatestSettings : GlobalSettings
{
    [CommandArgument(0, "<ID>")]
    [Description("Item ID")]
    public string Id { get; set; } = string.Empty;

    [CommandOption("--parent <ID>")]
    [Description("Override the parent library or folder used for latest ranking")]
    public string? Parent { get; set; }

    [CommandOption("--visible-limit <N>")]
    [Description("Visible shelf size to compare against")]
    public int VisibleLimit { get; set; } = 20;

    [CommandOption("--probe-limit <N>")]
    [Description("How many latest items to inspect when computing rank")]
    public int ProbeLimit { get; set; } = 200;

    public override ValidationResult Validate()
    {
        if (!Guid.TryParse(Id, out _))
            return ValidationResult.Error("A valid item ID is required.");

        if (!string.IsNullOrWhiteSpace(Parent) && !Guid.TryParse(Parent, out _))
            return ValidationResult.Error("A valid parent ID is required.");

        if (VisibleLimit < 1)
            return ValidationResult.Error("--visible-limit must be at least 1.");

        if (ProbeLimit < 1)
            return ValidationResult.Error("--probe-limit must be at least 1.");

        return ValidationResult.Success();
    }
}

public sealed class ItemsExplainLatestCommand : ApiCommand<ItemsExplainLatestSettings>
{
    public ItemsExplainLatestCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ItemsExplainLatestSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var itemId = Guid.Parse(settings.Id);
        var userId = await ResolveOptionalUserIdAsync(settings, client, cancellationToken);
        var item = await ItemDiagnosticHelpers.GetDiagnosticItemAsync(client, itemId, userId, cancellationToken);

        if (item is null)
        {
            AnsiConsole.MarkupLine("[red]Item not found.[/]");
            return 1;
        }

        var ancestors = await client.Items[itemId].Ancestors.GetAsync(config =>
        {
            config.QueryParameters.UserId = userId;
        }, cancellationToken) ?? [];

        var parentLibrary = ResolveLatestParent(settings.Parent, item, ancestors);
        var parentLibraryName = ResolveLatestParentName(parentLibrary, item, ancestors);
        var latestItems = await client.Items.Latest.GetAsync(config =>
        {
            var q = config.QueryParameters;
            q.UserId = userId;
            q.ParentId = parentLibrary;
            q.Limit = settings.ProbeLimit;
            q.GroupItems = true;
            q.EnableUserData = userId is not null;
            q.FieldsAsItemFields = ItemDiagnosticHelpers.DiagnosticFields;
        }, cancellationToken) ?? [];

        UserDto? user = null;
        if (userId is Guid resolvedUserId)
            user = await client.Users[resolvedUserId].GetAsync(cancellationToken: cancellationToken);

        var containerId = ItemDiagnosticHelpers.GetLatestContainerId(item);
        var eligible = ItemDiagnosticHelpers.IsEligibleForLatest(item) && containerId is not null;
        var exactRank = FindRank(latestItems, latest => latest.Id == item.Id);
        var containerRank = FindRank(latestItems, latest => ItemDiagnosticHelpers.GetLatestContainerId(latest) == containerId);
        var containerRepresentative = containerRank is int rank && rank > 0 ? latestItems[rank - 1] : null;
        var libraryExcluded = parentLibrary is Guid latestParentId &&
                              (user?.Configuration?.LatestItemsExcludes?.Any(excluded => excluded == latestParentId) ?? false);
        var playedHidden = user?.Configuration?.HidePlayedInLatest == true && item.UserData?.Played == true;

        var reasons = BuildReasons(
            item,
            eligible,
            libraryExcluded,
            playedHidden,
            exactRank,
            containerRank,
            containerRepresentative,
            settings.VisibleLimit,
            settings.ProbeLimit,
            latestItems.Count);

        if (settings.Json)
        {
            OutputHelper.WriteJson(new
            {
                id = item.Id,
                name = item.Name,
                type = item.Type?.ToString(),
                eligible,
                latestContainerId = containerId,
                latestContainer = ItemDiagnosticHelpers.DescribeLatestContainer(item),
                parentLibraryId = parentLibrary,
                parentLibrary = parentLibraryName,
                dateCreated = item.DateCreated,
                dateLastMediaAdded = item.DateLastMediaAdded,
                rankWithinLatest = containerRank ?? exactRank,
                exactItemRank = exactRank,
                containerRank,
                visibleLimit = settings.VisibleLimit,
                probeLimit = settings.ProbeLimit,
                returnedCount = latestItems.Count,
                excludedByUserPreferences = libraryExcluded || playedHidden,
                userPreferenceDetails = new
                {
                    hidePlayedInLatest = user?.Configuration?.HidePlayedInLatest,
                    latestItemsExcludes = user?.Configuration?.LatestItemsExcludes,
                    itemPlayed = item.UserData?.Played,
                },
                visibleRepresentative = containerRepresentative is null
                    ? null
                    : new
                    {
                        id = containerRepresentative.Id,
                        name = containerRepresentative.Name,
                        type = containerRepresentative.Type?.ToString(),
                    },
                reasons,
            });
            return 0;
        }

        var table = OutputHelper.CreateTable("Field", "Value");
        table.AddRow("Id", item.Id?.ToString() ?? string.Empty);
        table.AddRow("Name", Markup.Escape(item.Name ?? "(untitled)"));
        table.AddRow("Type", item.Type?.ToString() ?? string.Empty);
        table.AddRow("Eligible For Latest", eligible.ToString());
        table.AddRow("Latest Container Id", containerId?.ToString() ?? string.Empty);
        table.AddRow("Latest Container", Markup.Escape(ItemDiagnosticHelpers.DescribeLatestContainer(item)));
        table.AddRow("Parent Library Id", parentLibrary?.ToString() ?? string.Empty);
        table.AddRow("Parent Library", Markup.Escape(parentLibraryName ?? string.Empty));
        table.AddRow("DateCreated", ItemDiagnosticHelpers.FormatDate(item.DateCreated));
        table.AddRow("DateLastMediaAdded", ItemDiagnosticHelpers.FormatDate(item.DateLastMediaAdded));
        table.AddRow("Rank Within Latest", (containerRank ?? exactRank)?.ToString() ?? $"Not found in first {settings.ProbeLimit}");
        table.AddRow("Exact Item Rank", exactRank?.ToString() ?? string.Empty);
        table.AddRow("Container Rank", containerRank?.ToString() ?? string.Empty);
        table.AddRow("Excluded By User Preferences", (libraryExcluded || playedHidden).ToString());
        table.AddRow("HidePlayedInLatest", user?.Configuration?.HidePlayedInLatest?.ToString() ?? string.Empty);
        table.AddRow("Item Played", item.UserData?.Played?.ToString() ?? string.Empty);
        table.AddRow("Visible Representative", Markup.Escape(containerRepresentative?.Name ?? string.Empty));

        OutputHelper.WriteTable(table);

        foreach (var reason in reasons)
            AnsiConsole.MarkupLine($"[dim]- {Markup.Escape(reason)}[/]");

        return 0;
    }

    private static Guid? ResolveLatestParent(string? explicitParent, BaseItemDto item, IReadOnlyList<BaseItemDto> ancestors)
    {
        if (Guid.TryParse(explicitParent, out var explicitParentId))
            return explicitParentId;

        var libraryAncestor = ancestors.LastOrDefault(ancestor => ancestor.Type is BaseItemDto_Type.CollectionFolder or BaseItemDto_Type.UserView);
        if (libraryAncestor?.Id is Guid libraryId)
            return libraryId;

        var topAncestor = ancestors.LastOrDefault(ancestor => ancestor.Type != BaseItemDto_Type.UserRootFolder);
        if (topAncestor?.Id is Guid ancestorId)
            return ancestorId;

        return item.ParentId;
    }

    private static string? ResolveLatestParentName(Guid? latestParentId, BaseItemDto item, IReadOnlyList<BaseItemDto> ancestors)
    {
        return ancestors.FirstOrDefault(ancestor => ancestor.Id == latestParentId)?.Name
            ?? (item.ParentId == latestParentId ? item.Name : null);
    }

    private static int? FindRank(IReadOnlyList<BaseItemDto> items, Func<BaseItemDto, bool> predicate)
    {
        for (var index = 0; index < items.Count; index++)
        {
            if (predicate(items[index]))
                return index + 1;
        }

        return null;
    }

    private static List<string> BuildReasons(
        BaseItemDto item,
        bool eligible,
        bool libraryExcluded,
        bool playedHidden,
        int? exactRank,
        int? containerRank,
        BaseItemDto? containerRepresentative,
        int visibleLimit,
        int probeLimit,
        int returnedCount)
    {
        var reasons = new List<string>();

        if (libraryExcluded)
            reasons.Add("The parent library is excluded by the user's LatestItemsExcludes preference.");

        if (playedHidden)
            reasons.Add("The item is marked played and the user hides played items in latest shelves.");

        if (!eligible)
            reasons.Add($"Items of type '{item.Type}' are not typically eligible for Jellyfin's latest shelf.");

        if (exactRank is int visibleExactRank && visibleExactRank <= visibleLimit)
            reasons.Add($"The exact item is visible in the current shelf at rank {visibleExactRank}.");
        else if (containerRank is int visibleContainerRank && visibleContainerRank <= visibleLimit)
        {
            if (containerRepresentative?.Id != item.Id)
                reasons.Add($"The item is grouped into the visible latest container at rank {visibleContainerRank}; Jellyfin shows '{containerRepresentative?.Name}' instead.");
            else
                reasons.Add($"The item's latest container is visible at rank {visibleContainerRank}.");
        }
        else if (containerRank is int hiddenContainerRank)
        {
            reasons.Add($"The item's latest container ranks {hiddenContainerRank}, which is outside the first {visibleLimit} visible shelf slots.");
        }
        else if (exactRank is int hiddenExactRank)
        {
            reasons.Add($"The exact item ranks {hiddenExactRank}, which is outside the first {visibleLimit} visible shelf slots.");
        }
        else if (returnedCount >= probeLimit)
        {
            reasons.Add($"The item was not found in the first {probeLimit} grouped latest results. Increase --probe-limit to search deeper.");
        }
        else
        {
            reasons.Add("The item is not returned by Jellyfin's grouped latest query for the resolved parent library.");
        }

        return reasons;
    }
}

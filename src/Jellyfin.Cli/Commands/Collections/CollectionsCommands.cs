using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Microsoft.Kiota.Abstractions;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Collections;

// ===========================================================================
// Create a collection
// ===========================================================================

public sealed class CollectionsCreateSettings : GlobalSettings
{
    [CommandOption("--name <NAME>")]
    [Description("Name for the new collection")]
    public string Name { get; set; } = string.Empty;

    [CommandOption("--items <IDS>")]
    [Description("Comma-separated item IDs to add to the collection")]
    public string? Items { get; set; }

    [CommandOption("--parent-id <ID>")]
    [Description("Create the collection within a specific folder")]
    public string? ParentId { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return ValidationResult.Error("--name is required.");

        if (!string.IsNullOrWhiteSpace(ParentId) && !Guid.TryParse(ParentId, out _))
            return ValidationResult.Error("--parent-id must be a valid GUID.");

        return ValidationResult.Success();
    }
}

public sealed class CollectionsCreateCommand : ApiCommand<CollectionsCreateSettings>
{
    public CollectionsCreateCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, CollectionsCreateSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        string[]? ids = null;
        if (!string.IsNullOrWhiteSpace(settings.Items))
        {
            ids = settings.Items
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        Guid? parentId = null;
        if (!string.IsNullOrWhiteSpace(settings.ParentId))
        {
            parentId = Guid.Parse(settings.ParentId);
        }

        var result = await client.Collections.PostAsync(cfg =>
        {
            cfg.QueryParameters.Name = settings.Name;
            cfg.QueryParameters.Ids = ids;
            cfg.QueryParameters.ParentId = parentId;
        });

        if (result is null)
        {
            AnsiConsole.MarkupLine("[red]Failed to create collection.[/]");
            return 1;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(new { id = result.Id, name = settings.Name });
            return 0;
        }

        AnsiConsole.MarkupLine(
            $"[green]Collection '[white]{settings.Name}[/]' created (ID: {result.Id}).[/]");
        return 0;
    }
}

// ===========================================================================
// Add items to a collection
// ===========================================================================

public sealed class CollectionsAddSettings : GlobalSettings
{
    [CommandArgument(0, "<COLLECTION_ID>")]
    [Description("The collection ID")]
    public string CollectionId { get; set; } = string.Empty;

    [CommandOption("--items <IDS>")]
    [Description("Comma-separated item IDs to add")]
    public string Items { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        if (!Guid.TryParse(CollectionId, out _))
            return ValidationResult.Error("COLLECTION_ID must be a valid GUID.");

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

public sealed class CollectionsAddCommand : ApiCommand<CollectionsAddSettings>
{
    public CollectionsAddCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, CollectionsAddSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var collectionId = Guid.Parse(settings.CollectionId);
        var ids = settings.Items
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => (Guid?)Guid.Parse(s))
            .ToArray();

        await client.Collections[collectionId].Items.PostAsync(cfg =>
        {
            cfg.QueryParameters.Ids = ids;
        });

        AnsiConsole.MarkupLine(
            $"[green]Added {ids.Length} item(s) to collection '{settings.CollectionId}'.[/]");
        return 0;
    }
}

// ===========================================================================
// Remove items from a collection
// ===========================================================================

public sealed class CollectionsRemoveSettings : GlobalSettings
{
    [CommandArgument(0, "<COLLECTION_ID>")]
    [Description("The collection ID")]
    public string CollectionId { get; set; } = string.Empty;

    [CommandOption("--items <IDS>")]
    [Description("Comma-separated item IDs to remove")]
    public string Items { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        if (!Guid.TryParse(CollectionId, out _))
            return ValidationResult.Error("COLLECTION_ID must be a valid GUID.");

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

public sealed class CollectionsRemoveCommand : ApiCommand<CollectionsRemoveSettings>
{
    public CollectionsRemoveCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, CollectionsRemoveSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var collectionId = Guid.Parse(settings.CollectionId);
        var ids = settings.Items
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => (Guid?)Guid.Parse(s))
            .ToArray();

        if (!settings.Yes)
        {
            if (!OutputHelper.Confirm(
                $"Remove {ids.Length} item(s) from collection '{settings.CollectionId}'?"))
            {
                AnsiConsole.MarkupLine("[yellow]Cancelled.[/]");
                return 0;
            }
        }

        await client.Collections[collectionId].Items.DeleteAsync(cfg =>
        {
            cfg.QueryParameters.Ids = ids;
        });

        AnsiConsole.MarkupLine(
            $"[green]Removed {ids.Length} item(s) from collection '{settings.CollectionId}'.[/]");
        return 0;
    }
}

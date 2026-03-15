using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Items;

public sealed class ItemsTreeSettings : GlobalSettings
{
    [CommandArgument(0, "<ID>")]
    [Description("Root item ID")]
    public string Id { get; set; } = string.Empty;

    [CommandOption("--dates")]
    [Description("Include date columns useful for latest-items diagnostics")]
    public bool Dates { get; set; }

    public override ValidationResult Validate()
    {
        return Guid.TryParse(Id, out _)
            ? ValidationResult.Success()
            : ValidationResult.Error("A valid item ID is required.");
    }
}

public sealed class ItemsTreeCommand : ApiCommand<ItemsTreeSettings>
{
    public ItemsTreeCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ItemsTreeSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var itemId = Guid.Parse(settings.Id);
        var userId = await ResolveOptionalUserIdAsync(settings, client, cancellationToken);
        var item = await ItemDiagnosticHelpers.GetDiagnosticItemAsync(client, itemId, userId, cancellationToken);

        if (item is null)
        {
            AnsiConsole.MarkupLine("[red]Item not found.[/]");
            return 1;
        }

        using var httpClient = CreateHttpClient();
        var entries = await ItemDiagnosticHelpers.BuildTreeAsync(client, httpClient, item, userId, settings.Dates, cancellationToken);

        if (settings.Json)
        {
            OutputHelper.WriteJson(new
            {
                rootId = item.Id,
                items = entries.Select(entry => new
                {
                    depth = entry.Depth,
                    id = entry.Item.Id,
                    name = entry.Item.Name,
                    type = entry.Item.Type?.ToString(),
                    dateCreated = entry.Item.DateCreated,
                    dateModified = entry.DateModified,
                    premiereDate = entry.Item.PremiereDate,
                    path = entry.Item.Path,
                }),
            });
            return 0;
        }

        var table = settings.Dates
            ? OutputHelper.CreateTable("Node", "Type", "DateCreated", "DateModified", "PremiereDate", "Path")
            : OutputHelper.CreateTable("Node", "Type", "Id", "Path");

        foreach (var entry in entries)
        {
            if (settings.Dates)
            {
                table.AddRow(
                    ItemDiagnosticHelpers.FormatNodeLabel(entry.Item, entry.Depth),
                    entry.Item.Type?.ToString() ?? string.Empty,
                    ItemDiagnosticHelpers.FormatDate(entry.Item.DateCreated),
                    ItemDiagnosticHelpers.FormatDate(entry.DateModified),
                    ItemDiagnosticHelpers.FormatDate(entry.Item.PremiereDate),
                    Markup.Escape(entry.Item.Path ?? string.Empty));
                continue;
            }

            table.AddRow(
                ItemDiagnosticHelpers.FormatNodeLabel(entry.Item, entry.Depth),
                entry.Item.Type?.ToString() ?? string.Empty,
                entry.Item.Id?.ToString() ?? string.Empty,
                Markup.Escape(entry.Item.Path ?? string.Empty));
        }

        OutputHelper.WriteTable(table);
        return 0;
    }
}

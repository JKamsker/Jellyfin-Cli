using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Items;

public sealed class ItemsAncestorsSettings : GlobalSettings
{
    [CommandArgument(0, "<ID>")]
    [Description("Item ID")]
    public string Id { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        return Guid.TryParse(Id, out _)
            ? ValidationResult.Success()
            : ValidationResult.Error("A valid item ID (GUID) is required.");
    }
}

public sealed class ItemsAncestorsCommand : ApiCommand<ItemsAncestorsSettings>
{
    public ItemsAncestorsCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ItemsAncestorsSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var userId = await ResolveOptionalUserIdAsync(settings, client, cancellationToken);
        var ancestors = await client.Items[Guid.Parse(settings.Id)].Ancestors.GetAsync(config =>
        {
            config.QueryParameters.UserId = userId;
        }, cancellationToken);

        if (ancestors is not { Count: > 0 })
        {
            AnsiConsole.MarkupLine("[yellow]No ancestors found.[/]");
            return 0;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(ancestors);
            return 0;
        }

        var table = OutputHelper.CreateTable("Level", "Id", "Name", "Type");
        for (var index = 0; index < ancestors.Count; index++)
        {
            var ancestor = ancestors[index];
            table.AddRow(
                (index + 1).ToString(),
                ancestor.Id?.ToString() ?? string.Empty,
                Markup.Escape(ancestor.Name ?? "(untitled)"),
                ancestor.Type?.ToString() ?? string.Empty);
        }

        OutputHelper.WriteTable(table);
        return 0;
    }
}

using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Items;

public sealed class ItemsRemoteImagesProvidersSettings : GlobalSettings
{
    [CommandArgument(0, "<ITEM_ID>")]
    [Description("Item ID")]
    public string ItemId { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        return Guid.TryParse(ItemId, out _)
            ? ValidationResult.Success()
            : ValidationResult.Error("A valid item ID (GUID) is required.");
    }
}

public sealed class ItemsRemoteImagesProvidersCommand : ApiCommand<ItemsRemoteImagesProvidersSettings>
{
    public ItemsRemoteImagesProvidersCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ItemsRemoteImagesProvidersSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var providers = await client.Items[Guid.Parse(settings.ItemId)].RemoteImages.Providers.GetAsync(cancellationToken: cancellationToken);

        if (providers is null || providers.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No remote image providers found.[/]");
            return 0;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(providers);
            return 0;
        }

        var table = OutputHelper.CreateTable("Provider", "Supported Images");
        foreach (var provider in providers)
        {
            table.AddRow(
                Markup.Escape(provider.Name ?? string.Empty),
                string.Join(", ", provider.SupportedImages?.Select(image => image?.ToString()) ?? []));
        }

        OutputHelper.WriteTable(table);
        return 0;
    }
}

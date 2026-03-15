using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Items;

public sealed class ItemsRemoteImagesListSettings : GlobalSettings
{
    [CommandArgument(0, "<ITEM_ID>")]
    [Description("Item ID")]
    public string ItemId { get; set; } = string.Empty;

    [CommandOption("--type <TYPE>")]
    [Description("Image type filter")]
    public string? ImageType { get; set; }

    [CommandOption("--provider <NAME>")]
    [Description("Remote provider name")]
    public string? ProviderName { get; set; }

    [CommandOption("--all-languages")]
    [Description("Include images from all languages")]
    public bool IncludeAllLanguages { get; set; }

    public override ValidationResult Validate()
    {
        if (!Guid.TryParse(ItemId, out _))
            return ValidationResult.Error("A valid item ID (GUID) is required.");

        return ValidationResult.Success();
    }
}

public sealed class ItemsRemoteImagesListCommand : ApiCommand<ItemsRemoteImagesListSettings>
{
    public ItemsRemoteImagesListCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ItemsRemoteImagesListSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var result = await client.Items[Guid.Parse(settings.ItemId)].RemoteImages.GetAsync(config =>
        {
            config.QueryParameters.StartIndex = settings.Start;
            config.QueryParameters.Limit = settings.Limit ?? 25;
            config.QueryParameters.ProviderName = settings.ProviderName;
            config.QueryParameters.IncludeAllLanguages = settings.IncludeAllLanguages ? true : null;

            if (!string.IsNullOrWhiteSpace(settings.ImageType) &&
                Enum.TryParse<Jellyfin.Cli.Api.Generated.Items.Item.RemoteImages.ImageType>(settings.ImageType, true, out var imageType))
            {
                config.QueryParameters.TypeAsImageType = imageType;
            }
        }, cancellationToken);

        var images = result?.Images ?? [];
        if (images.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No remote images found.[/]");
            return 0;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(result);
            return 0;
        }

        var table = OutputHelper.CreateTable("Provider", "Type", "Language", "Rating", "Votes", "Size", "Url");
        foreach (var image in images)
        {
            table.AddRow(
                Markup.Escape(image.ProviderName ?? string.Empty),
                image.Type?.ToString() ?? string.Empty,
                Markup.Escape(image.Language ?? string.Empty),
                image.CommunityRating?.ToString("0.0") ?? string.Empty,
                image.VoteCount?.ToString() ?? string.Empty,
                image.Width is not null && image.Height is not null ? $"{image.Width}x{image.Height}" : string.Empty,
                Markup.Escape(image.Url ?? string.Empty));
        }

        OutputHelper.WriteTable(table);
        if (result?.TotalRecordCount is int totalRecordCount)
            AnsiConsole.MarkupLine($"[dim]Showing {images.Count} of {totalRecordCount} images[/]");

        return 0;
    }
}

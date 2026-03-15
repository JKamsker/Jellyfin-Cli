using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Items;

public sealed class ItemsRemoteImagesDownloadSettings : GlobalSettings
{
    [CommandArgument(0, "<ITEM_ID>")]
    [Description("Item ID")]
    public string ItemId { get; set; } = string.Empty;

    [CommandOption("--url <URL>")]
    [Description("Remote image URL to download")]
    public string? ImageUrl { get; set; }

    [CommandOption("--type <TYPE>")]
    [Description("Image type to download")]
    public string? ImageType { get; set; }

    public override ValidationResult Validate()
    {
        if (!Guid.TryParse(ItemId, out _))
            return ValidationResult.Error("A valid item ID (GUID) is required.");

        if (string.IsNullOrWhiteSpace(ImageUrl))
            return ValidationResult.Error("--url is required.");

        return string.IsNullOrWhiteSpace(ImageType)
            ? ValidationResult.Error("--type is required.")
            : ValidationResult.Success();
    }
}

public sealed class ItemsRemoteImagesDownloadCommand : ApiCommand<ItemsRemoteImagesDownloadSettings>
{
    public ItemsRemoteImagesDownloadCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ItemsRemoteImagesDownloadSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<Jellyfin.Cli.Api.Generated.Items.Item.RemoteImages.Download.ImageType>(settings.ImageType, true, out var imageType))
        {
            AnsiConsole.MarkupLine($"[red]Unknown image type '{Markup.Escape(settings.ImageType!)}'.[/]");
            return 1;
        }

        await client.Items[Guid.Parse(settings.ItemId)].RemoteImages.Download.PostAsync(config =>
        {
            config.QueryParameters.ImageUrl = settings.ImageUrl;
            config.QueryParameters.TypeAsImageType = imageType;
        }, cancellationToken);

        AnsiConsole.MarkupLine($"[green]Downloaded remote image for item [white]{settings.ItemId}[/].[/]");
        return 0;
    }
}

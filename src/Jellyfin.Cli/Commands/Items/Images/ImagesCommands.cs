using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Items.Images;

// ───────────────────── List ─────────────────────

public sealed class ImagesListSettings : GlobalSettings
{
    [CommandArgument(0, "<ID>")]
    [Description("Item ID")]
    public string Id { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        if (!Guid.TryParse(Id, out _))
            return ValidationResult.Error("A valid item ID (GUID) is required.");
        return ValidationResult.Success();
    }
}

public sealed class ImagesListCommand : ApiCommand<ImagesListSettings>
{
    public ImagesListCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ImagesListSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var itemId = Guid.Parse(settings.Id);
        var images = await client.Items[itemId].Images.GetAsync();

        if (images is null || images.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No images found for this item.[/]");
            return 0;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(images.Select(img => new
            {
                imageType = img.ImageType?.ToString(),
                imageIndex = img.ImageIndex,
                width = img.Width,
                height = img.Height,
                size = img.Size,
                imageTag = img.ImageTag,
            }));
            return 0;
        }

        var table = OutputHelper.CreateTable("Type", "Index", "Width", "Height", "Size", "Tag");
        foreach (var img in images)
        {
            table.AddRow(
                img.ImageType?.ToString() ?? "",
                img.ImageIndex?.ToString() ?? "0",
                img.Width?.ToString() ?? "",
                img.Height?.ToString() ?? "",
                FormatSize(img.Size),
                img.ImageTag ?? "");
        }

        OutputHelper.WriteTable(table);
        return 0;
    }

    private static string FormatSize(long? bytes)
    {
        if (bytes is null)
            return "";
        return bytes.Value switch
        {
            >= 1_048_576 => $"{bytes.Value / 1_048_576.0:F1} MB",
            >= 1_024 => $"{bytes.Value / 1_024.0:F0} KB",
            _ => $"{bytes.Value} B",
        };
    }
}

// ───────────────────── Set (upload) ─────────────────────

public sealed class ImagesSetSettings : GlobalSettings
{
    [CommandArgument(0, "<ITEM_ID>")]
    [Description("Item ID")]
    public string Id { get; set; } = string.Empty;

    [CommandArgument(1, "<IMAGE_TYPE>")]
    [Description("Image type (e.g. Primary, Backdrop, Logo, Thumb, Banner, Art, Disc)")]
    public string ImageType { get; set; } = string.Empty;

    [CommandArgument(2, "<FILE>")]
    [Description("Path to image file")]
    public string FilePath { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        if (!Guid.TryParse(Id, out _))
            return ValidationResult.Error("A valid item ID (GUID) is required.");

        if (string.IsNullOrWhiteSpace(ImageType))
            return ValidationResult.Error("Image type is required.");

        if (!File.Exists(FilePath))
            return ValidationResult.Error($"File not found: {FilePath}");

        return ValidationResult.Success();
    }
}

public sealed class ImagesSetCommand : ApiCommand<ImagesSetSettings>
{
    public ImagesSetCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ImagesSetSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var itemId = Guid.Parse(settings.Id);

        await using var fileStream = File.OpenRead(settings.FilePath);
        await client.Items[itemId].Images[settings.ImageType].PostAsync(fileStream);

        AnsiConsole.MarkupLine(
            $"[green]Uploaded {Markup.Escape(settings.ImageType)} image for item {settings.Id}.[/]");
        return 0;
    }
}

// ───────────────────── Delete ─────────────────────

public sealed class ImagesDeleteSettings : GlobalSettings
{
    [CommandArgument(0, "<ITEM_ID>")]
    [Description("Item ID")]
    public string Id { get; set; } = string.Empty;

    [CommandArgument(1, "<IMAGE_TYPE>")]
    [Description("Image type (e.g. Primary, Backdrop, Logo, Thumb, Banner, Art, Disc)")]
    public string ImageType { get; set; } = string.Empty;

    [CommandOption("--index <INDEX>")]
    [Description("Image index (defaults to 0)")]
    public int? ImageIndex { get; set; }

    public override ValidationResult Validate()
    {
        if (!Guid.TryParse(Id, out _))
            return ValidationResult.Error("A valid item ID (GUID) is required.");

        if (string.IsNullOrWhiteSpace(ImageType))
            return ValidationResult.Error("Image type is required.");

        return ValidationResult.Success();
    }
}

public sealed class ImagesDeleteCommand : ApiCommand<ImagesDeleteSettings>
{
    public ImagesDeleteCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ImagesDeleteSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var itemId = Guid.Parse(settings.Id);

        if (!settings.Yes)
        {
            var confirmed = OutputHelper.Confirm(
                $"Delete {settings.ImageType} image (index {settings.ImageIndex ?? 0}) from item {settings.Id}?");
            if (!confirmed)
            {
                AnsiConsole.MarkupLine("[yellow]Cancelled.[/]");
                return 0;
            }
        }

        await client.Items[itemId].Images[settings.ImageType].DeleteAsync(config =>
        {
            config.QueryParameters.ImageIndex = settings.ImageIndex ?? 0;
        });

        AnsiConsole.MarkupLine(
            $"[green]Deleted {Markup.Escape(settings.ImageType)} image (index {settings.ImageIndex ?? 0}) from item {settings.Id}.[/]");
        return 0;
    }
}

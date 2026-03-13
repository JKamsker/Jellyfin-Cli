using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using MetadataRefreshMode = Jellyfin.Cli.Api.Generated.Items.Item.Refresh.MetadataRefreshMode;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Items;

public sealed class ItemsRefreshSettings : GlobalSettings
{
    [CommandArgument(0, "<ID>")]
    [Description("Item ID to refresh")]
    public string Id { get; set; } = string.Empty;

    [CommandOption("--mode <MODE>")]
    [Description("Refresh mode: default, full, or validation")]
    public string? Mode { get; set; }

    [CommandOption("--replace-images")]
    [Description("Replace all images during full refresh")]
    public bool ReplaceImages { get; set; }

    [CommandOption("--replace-metadata")]
    [Description("Replace all metadata during full refresh")]
    public bool ReplaceMetadata { get; set; }

    [CommandOption("--regenerate-trickplay")]
    [Description("Regenerate trickplay images during full refresh")]
    public bool RegenerateTrickplay { get; set; }

    public override ValidationResult Validate()
    {
        if (!Guid.TryParse(Id, out _))
            return ValidationResult.Error("A valid item ID (GUID) is required.");

        if (Mode is not null)
        {
            var normalized = Mode.ToLowerInvariant();
            if (normalized is not ("default" or "full" or "validation" or "none"))
                return ValidationResult.Error("Mode must be 'default', 'full', 'validation', or 'none'.");
        }

        return ValidationResult.Success();
    }
}

public sealed class ItemsRefreshCommand : ApiCommand<ItemsRefreshSettings>
{
    public ItemsRefreshCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ItemsRefreshSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var itemId = Guid.Parse(settings.Id);

        await client.Items[itemId].Refresh.PostAsync(config =>
        {
            var q = config.QueryParameters;

            if (settings.Mode is not null)
            {
                var mode = settings.Mode.ToLowerInvariant() switch
                {
                    "full" => MetadataRefreshMode.FullRefresh,
                    "validation" => MetadataRefreshMode.ValidationOnly,
                    "none" => MetadataRefreshMode.None,
                    _ => MetadataRefreshMode.Default,
                };
                q.MetadataRefreshModeAsMetadataRefreshMode = mode;
                q.ImageRefreshModeAsMetadataRefreshMode = mode;
            }

            q.ReplaceAllImages = settings.ReplaceImages;
            q.ReplaceAllMetadata = settings.ReplaceMetadata;
            q.RegenerateTrickplay = settings.RegenerateTrickplay;
        });

        AnsiConsole.MarkupLine($"[green]Refresh queued for item {settings.Id}.[/]");
        return 0;
    }
}

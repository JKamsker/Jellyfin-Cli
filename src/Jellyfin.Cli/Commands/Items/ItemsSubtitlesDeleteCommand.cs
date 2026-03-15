using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Items;

public sealed class ItemsSubtitlesDeleteSettings : GlobalSettings
{
    [CommandArgument(0, "<ITEM_ID>")]
    [Description("Item ID")]
    public string ItemId { get; set; } = string.Empty;

    [CommandArgument(1, "<INDEX>")]
    [Description("Subtitle index")]
    public int Index { get; set; }

    public override ValidationResult Validate()
    {
        if (!Guid.TryParse(ItemId, out _))
            return ValidationResult.Error("A valid item ID (GUID) is required.");

        return Index < 0
            ? ValidationResult.Error("Subtitle index must be zero or greater.")
            : ValidationResult.Success();
    }
}

public sealed class ItemsSubtitlesDeleteCommand : ApiCommand<ItemsSubtitlesDeleteSettings>
{
    public ItemsSubtitlesDeleteCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ItemsSubtitlesDeleteSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        if (!settings.Yes)
        {
            var confirmed = OutputHelper.Confirm(
                $"Delete subtitle index {settings.Index} from item {settings.ItemId}?");
            if (!confirmed)
            {
                AnsiConsole.MarkupLine("[yellow]Cancelled.[/]");
                return 0;
            }
        }

        await client.Videos[Guid.Parse(settings.ItemId)].Subtitles[settings.Index].DeleteAsync(cancellationToken: cancellationToken);
        AnsiConsole.MarkupLine($"[green]Deleted subtitle index [white]{settings.Index}[/].[/]");
        return 0;
    }
}

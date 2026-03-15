using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Items;

public sealed class ItemsLyricsDeleteSettings : GlobalSettings
{
    [CommandArgument(0, "<ITEM_ID>")]
    [Description("Audio item ID")]
    public string ItemId { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        return Guid.TryParse(ItemId, out _)
            ? ValidationResult.Success()
            : ValidationResult.Error("A valid audio item ID (GUID) is required.");
    }
}

public sealed class ItemsLyricsDeleteCommand : ApiCommand<ItemsLyricsDeleteSettings>
{
    public ItemsLyricsDeleteCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ItemsLyricsDeleteSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        if (!settings.Yes)
        {
            var confirmed = OutputHelper.Confirm($"Delete external lyrics from item {settings.ItemId}?");
            if (!confirmed)
            {
                AnsiConsole.MarkupLine("[yellow]Cancelled.[/]");
                return 0;
            }
        }

        await client.Audio[Guid.Parse(settings.ItemId)].Lyrics.DeleteAsync(cancellationToken: cancellationToken);
        AnsiConsole.MarkupLine($"[green]Deleted external lyrics from item [white]{settings.ItemId}[/].[/]");
        return 0;
    }
}

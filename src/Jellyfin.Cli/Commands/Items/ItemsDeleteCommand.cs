using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Items;

public sealed class ItemsDeleteSettings : GlobalSettings
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

public sealed class ItemsDeleteCommand : ApiCommand<ItemsDeleteSettings>
{
    public ItemsDeleteCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ItemsDeleteSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        if (!settings.Yes)
        {
            var confirmed = OutputHelper.Confirm(
                $"Delete item '{settings.Id}' from the library and filesystem?");
            if (!confirmed)
            {
                AnsiConsole.MarkupLine("[yellow]Cancelled.[/]");
                return 0;
            }
        }

        await client.Items[Guid.Parse(settings.Id)].DeleteAsync(cancellationToken: cancellationToken);
        AnsiConsole.MarkupLine($"[green]Deleted item [white]{settings.Id}[/].[/]");
        return 0;
    }
}

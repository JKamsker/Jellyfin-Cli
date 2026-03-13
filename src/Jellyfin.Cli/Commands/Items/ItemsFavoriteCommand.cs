using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Items;

public sealed class ItemsFavoriteSettings : GlobalSettings
{
    [CommandArgument(0, "<ACTION>")]
    [Description("Action: 'set' to favorite, 'unset' to unfavorite")]
    public string Action { get; set; } = string.Empty;

    [CommandArgument(1, "<ID>")]
    [Description("Item ID")]
    public string Id { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        var action = Action.ToLowerInvariant();
        if (action is not ("set" or "unset"))
            return ValidationResult.Error("Action must be 'set' or 'unset'.");

        if (!Guid.TryParse(Id, out _))
            return ValidationResult.Error("A valid item ID (GUID) is required.");

        return ValidationResult.Success();
    }
}

public sealed class ItemsFavoriteCommand : ApiCommand<ItemsFavoriteSettings>
{
    public ItemsFavoriteCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ItemsFavoriteSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var itemId = Guid.Parse(settings.Id);
        var isFavorite = settings.Action.Equals("set", StringComparison.OrdinalIgnoreCase);

        if (isFavorite)
        {
            var result = await client.UserFavoriteItems[itemId].PostAsync();
            AnsiConsole.MarkupLine($"[green]Item marked as favorite.[/]");

            if (settings.Json && result is not null)
            {
                OutputHelper.WriteJson(new
                {
                    isFavorite = result.IsFavorite,
                    itemId = settings.Id,
                });
            }
        }
        else
        {
            var result = await client.UserFavoriteItems[itemId].DeleteAsync();
            AnsiConsole.MarkupLine($"[green]Item removed from favorites.[/]");

            if (settings.Json && result is not null)
            {
                OutputHelper.WriteJson(new
                {
                    isFavorite = result.IsFavorite,
                    itemId = settings.Id,
                });
            }
        }

        return 0;
    }
}

using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Api.Generated.Models;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Items;

public sealed class ItemsSimilarSettings : GlobalSettings
{
    [CommandArgument(0, "<ID>")]
    [Description("Item ID")]
    public string Id { get; set; } = string.Empty;

    [CommandOption("--exclude-artist-id <ID>")]
    [Description("Artist IDs to exclude, can be repeated")]
    public string[]? ExcludeArtistIds { get; set; }

    public override ValidationResult Validate()
    {
        if (!Guid.TryParse(Id, out _))
            return ValidationResult.Error("A valid item ID (GUID) is required.");

        if (ExcludeArtistIds is not null && ExcludeArtistIds.Any(id => !Guid.TryParse(id, out _)))
            return ValidationResult.Error("--exclude-artist-id values must be valid GUIDs.");

        return ValidationResult.Success();
    }
}

public sealed class ItemsSimilarCommand : ApiCommand<ItemsSimilarSettings>
{
    public ItemsSimilarCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ItemsSimilarSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var userId = await ResolveOptionalUserIdAsync(settings, client, cancellationToken);
        var result = await client.Items[Guid.Parse(settings.Id)].Similar.GetAsync(config =>
        {
            config.QueryParameters.UserId = userId;
            config.QueryParameters.Limit = settings.Limit ?? 20;
            config.QueryParameters.FieldsAsItemFields =
            [
                ItemFields.Overview,
                ItemFields.Path,
            ];

            if (settings.ExcludeArtistIds is { Length: > 0 })
                config.QueryParameters.ExcludeArtistIds = settings.ExcludeArtistIds.Select(Guid.Parse).Cast<Guid?>().ToArray();
        }, cancellationToken);

        return ItemsCommandOutputHelper.WriteBaseItemQueryResult(settings, result, "No similar items found.");
    }
}

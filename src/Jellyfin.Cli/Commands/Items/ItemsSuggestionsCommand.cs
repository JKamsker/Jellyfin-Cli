using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Api.Generated.Models;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Items;

public sealed class ItemsSuggestionsSettings : GlobalSettings
{
    [CommandOption("--type <TYPE>")]
    [Description("Item type filter, comma-separated")]
    public string? ItemTypes { get; set; }

    [CommandOption("--media-type <TYPE>")]
    [Description("Media type filter, comma-separated")]
    public string? MediaTypes { get; set; }
}

public sealed class ItemsSuggestionsCommand : ApiCommand<ItemsSuggestionsSettings>
{
    public ItemsSuggestionsCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ItemsSuggestionsSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var userId = await ResolveOptionalUserIdAsync(settings, client, cancellationToken);
        var result = await client.Items.Suggestions.GetAsync(config =>
        {
            config.QueryParameters.UserId = userId;
            config.QueryParameters.StartIndex = settings.Start;
            config.QueryParameters.Limit = settings.Limit ?? 20;
            config.QueryParameters.EnableTotalRecordCount = true;

            if (!string.IsNullOrWhiteSpace(settings.MediaTypes))
            {
                config.QueryParameters.MediaTypeAsMediaType = settings.MediaTypes
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .Select(ParseMediaType)
                    .ToArray();
            }

            if (!string.IsNullOrWhiteSpace(settings.ItemTypes))
            {
                config.QueryParameters.TypeAsBaseItemKind = settings.ItemTypes
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .Select(ParseBaseItemKind)
                    .ToArray();
            }
        }, cancellationToken);

        return ItemsCommandOutputHelper.WriteBaseItemQueryResult(settings, result, "No suggestions found.");
    }

    private static MediaType ParseMediaType(string value)
    {
        return Enum.TryParse<MediaType>(value, true, out var parsed)
            ? parsed
            : throw new InvalidOperationException($"Unknown media type '{value}'.");
    }

    private static BaseItemKind ParseBaseItemKind(string value)
    {
        return Enum.TryParse<BaseItemKind>(value, true, out var parsed)
            ? parsed
            : throw new InvalidOperationException($"Unknown item type '{value}'.");
    }
}

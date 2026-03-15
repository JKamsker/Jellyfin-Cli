using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Commands.Items;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Browse;

public sealed class ArtistsListSettings : GlobalSettings
{
    [CommandOption("--album-artists")]
    [Description("List album artists instead of track artists")]
    public bool AlbumArtists { get; set; }

    [CommandOption("--search <TERM>")]
    [Description("Filter artists by name")]
    public string? SearchTerm { get; set; }

    [CommandOption("--parent <ID>")]
    [Description("Restrict results to one parent item or folder")]
    public string? ParentId { get; set; }

    public override ValidationResult Validate()
    {
        return string.IsNullOrWhiteSpace(ParentId) || Guid.TryParse(ParentId, out _)
            ? ValidationResult.Success()
            : ValidationResult.Error("--parent must be a valid GUID.");
    }
}

public sealed class ArtistsListCommand : ApiCommand<ArtistsListSettings>
{
    public ArtistsListCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ArtistsListSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var userId = await ResolveOptionalUserIdAsync(settings, client, cancellationToken);
        var parentId = ParseOptionalGuid(settings.ParentId);

        var result = settings.AlbumArtists
            ? await client.Artists.AlbumArtists.GetAsync(config =>
            {
                config.QueryParameters.UserId = userId;
                config.QueryParameters.ParentId = parentId;
                config.QueryParameters.SearchTerm = settings.SearchTerm;
                config.QueryParameters.StartIndex = settings.Start;
                config.QueryParameters.Limit = settings.Limit ?? 50;
                config.QueryParameters.EnableTotalRecordCount = true;
            }, cancellationToken)
            : await client.Artists.GetAsync(config =>
            {
                config.QueryParameters.UserId = userId;
                config.QueryParameters.ParentId = parentId;
                config.QueryParameters.SearchTerm = settings.SearchTerm;
                config.QueryParameters.StartIndex = settings.Start;
                config.QueryParameters.Limit = settings.Limit ?? 50;
                config.QueryParameters.EnableTotalRecordCount = true;
            }, cancellationToken);

        return ItemsCommandOutputHelper.WriteBaseItemQueryResult(settings, result, "No artists found.");
    }

    private static Guid? ParseOptionalGuid(string? value)
    {
        return Guid.TryParse(value, out var parsed) ? parsed : null;
    }
}

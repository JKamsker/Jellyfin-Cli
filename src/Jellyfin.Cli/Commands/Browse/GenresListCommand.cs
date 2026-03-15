using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Commands.Items;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Browse;

public sealed class GenresListSettings : GlobalSettings
{
    [CommandOption("--search <TERM>")]
    [Description("Filter genres by name")]
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

public sealed class GenresListCommand : ApiCommand<GenresListSettings>
{
    public GenresListCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, GenresListSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var userId = await ResolveOptionalUserIdAsync(settings, client, cancellationToken);
        var result = await client.Genres.GetAsync(config =>
        {
            config.QueryParameters.UserId = userId;
            config.QueryParameters.ParentId = ParseOptionalGuid(settings.ParentId);
            config.QueryParameters.SearchTerm = settings.SearchTerm;
            config.QueryParameters.StartIndex = settings.Start;
            config.QueryParameters.Limit = settings.Limit ?? 50;
            config.QueryParameters.EnableTotalRecordCount = true;
        }, cancellationToken);

        return ItemsCommandOutputHelper.WriteBaseItemQueryResult(settings, result, "No genres found.");
    }

    private static Guid? ParseOptionalGuid(string? value)
    {
        return Guid.TryParse(value, out var parsed) ? parsed : null;
    }
}

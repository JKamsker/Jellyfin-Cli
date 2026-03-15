using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Commands.Items;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Browse;

public sealed class PersonsListSettings : GlobalSettings
{
    [CommandOption("--search <TERM>")]
    [Description("Filter persons by name")]
    public string? SearchTerm { get; set; }

    [CommandOption("--type <TYPE>")]
    [Description("Person type filter, comma-separated")]
    public string? PersonTypes { get; set; }

    [CommandOption("--appears-in <ID>")]
    [Description("Restrict results to persons connected to one item")]
    public string? AppearsInItemId { get; set; }

    public override ValidationResult Validate()
    {
        return string.IsNullOrWhiteSpace(AppearsInItemId) || Guid.TryParse(AppearsInItemId, out _)
            ? ValidationResult.Success()
            : ValidationResult.Error("--appears-in must be a valid GUID.");
    }
}

public sealed class PersonsListCommand : ApiCommand<PersonsListSettings>
{
    public PersonsListCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, PersonsListSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var userId = await ResolveOptionalUserIdAsync(settings, client, cancellationToken);
        var result = await client.Persons.GetAsync(config =>
        {
            config.QueryParameters.UserId = userId;
            config.QueryParameters.SearchTerm = settings.SearchTerm;
            config.QueryParameters.Limit = settings.Limit ?? 50;

            if (!string.IsNullOrWhiteSpace(settings.PersonTypes))
            {
                config.QueryParameters.PersonTypes = settings.PersonTypes
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            }

            if (Guid.TryParse(settings.AppearsInItemId, out var itemId))
                config.QueryParameters.AppearsInItemId = itemId;
        }, cancellationToken);

        return ItemsCommandOutputHelper.WriteBaseItemQueryResult(settings, result, "No persons found.");
    }
}

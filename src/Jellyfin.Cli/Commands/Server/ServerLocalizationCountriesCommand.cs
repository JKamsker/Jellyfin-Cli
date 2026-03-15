using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Server;

public sealed class ServerLocalizationCountriesCommand : ApiCommand<GlobalSettings>
{
    public ServerLocalizationCountriesCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, GlobalSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var countries = await client.Localization.Countries.GetAsync(cancellationToken: cancellationToken) ?? [];

        if (countries.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No countries found.[/]");
            return 0;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(countries);
            return 0;
        }

        var table = OutputHelper.CreateTable("Name", "DisplayName", "ISO2", "ISO3");
        foreach (var country in countries.OrderBy(c => c.DisplayName ?? c.Name))
        {
            table.AddRow(
                country.Name ?? string.Empty,
                Markup.Escape(country.DisplayName ?? string.Empty),
                country.TwoLetterISORegionName ?? string.Empty,
                country.ThreeLetterISORegionName ?? string.Empty);
        }

        OutputHelper.WriteTable(table);
        return 0;
    }
}

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Server;

public sealed class ServerLocalizationCulturesCommand : ApiCommand<GlobalSettings>
{
    public ServerLocalizationCulturesCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, GlobalSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var cultures = await client.Localization.Cultures.GetAsync(cancellationToken: cancellationToken) ?? [];

        if (cultures.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No cultures found.[/]");
            return 0;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(cultures);
            return 0;
        }

        var table = OutputHelper.CreateTable("Name", "DisplayName", "ISO2", "ISO3");
        foreach (var culture in cultures.OrderBy(c => c.DisplayName ?? c.Name))
        {
            table.AddRow(
                culture.Name ?? string.Empty,
                Markup.Escape(culture.DisplayName ?? string.Empty),
                culture.TwoLetterISOLanguageName ?? string.Empty,
                culture.ThreeLetterISOLanguageName
                    ?? string.Join(", ", culture.ThreeLetterISOLanguageNames ?? []));
        }

        OutputHelper.WriteTable(table);
        return 0;
    }
}

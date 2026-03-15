using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Server;

public sealed class ServerLocalizationRatingsCommand : ApiCommand<GlobalSettings>
{
    public ServerLocalizationRatingsCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, GlobalSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var ratings = await client.Localization.ParentalRatings.GetAsync(cancellationToken: cancellationToken) ?? [];

        if (ratings.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No parental ratings found.[/]");
            return 0;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(ratings);
            return 0;
        }

        var table = OutputHelper.CreateTable("Name", "Value", "Score");
        foreach (var rating in ratings.OrderBy(r => r.Name))
        {
            var score = rating.RatingScore is null
                ? string.Empty
                : rating.RatingScore.SubScore is not null
                    ? $"{rating.RatingScore.Score}/{rating.RatingScore.SubScore}"
                    : rating.RatingScore.Score?.ToString() ?? string.Empty;

            table.AddRow(
                rating.Name ?? string.Empty,
                rating.Value?.ToString() ?? string.Empty,
                score);
        }

        OutputHelper.WriteTable(table);
        return 0;
    }
}

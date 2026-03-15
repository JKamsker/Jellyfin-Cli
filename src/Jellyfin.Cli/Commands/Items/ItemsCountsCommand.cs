using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Items;

public sealed class ItemsCountsSettings : GlobalSettings
{
    [CommandOption("--favorites")]
    [Description("Count only favorite items")]
    public bool FavoritesOnly { get; set; }
}

public sealed class ItemsCountsCommand : ApiCommand<ItemsCountsSettings>
{
    public ItemsCountsCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ItemsCountsSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var userId = await ResolveOptionalUserIdAsync(settings, client, cancellationToken);
        var counts = await client.Items.Counts.GetAsync(config =>
        {
            config.QueryParameters.UserId = userId;
            config.QueryParameters.IsFavorite = settings.FavoritesOnly ? true : null;
        }, cancellationToken);

        if (counts is null)
        {
            AnsiConsole.MarkupLine("[yellow]No item counts returned.[/]");
            return 0;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(counts);
            return 0;
        }

        var table = OutputHelper.CreateTable("Type", "Count");
        table.AddRow("Items", (counts.ItemCount ?? 0).ToString());
        table.AddRow("Movies", (counts.MovieCount ?? 0).ToString());
        table.AddRow("Series", (counts.SeriesCount ?? 0).ToString());
        table.AddRow("Episodes", (counts.EpisodeCount ?? 0).ToString());
        table.AddRow("Albums", (counts.AlbumCount ?? 0).ToString());
        table.AddRow("Songs", (counts.SongCount ?? 0).ToString());
        table.AddRow("Artists", (counts.ArtistCount ?? 0).ToString());
        table.AddRow("Books", (counts.BookCount ?? 0).ToString());
        table.AddRow("Box Sets", (counts.BoxSetCount ?? 0).ToString());
        table.AddRow("Music Videos", (counts.MusicVideoCount ?? 0).ToString());
        table.AddRow("Programs", (counts.ProgramCount ?? 0).ToString());
        table.AddRow("Trailers", (counts.TrailerCount ?? 0).ToString());
        OutputHelper.WriteTable(table);
        return 0;
    }
}

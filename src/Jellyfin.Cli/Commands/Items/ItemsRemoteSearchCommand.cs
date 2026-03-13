using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Api.Generated.Models;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Items;

public sealed class ItemsRemoteSearchSettings : GlobalSettings
{
    [CommandArgument(0, "<TERM>")]
    [Description("Search term")]
    public string Term { get; set; } = string.Empty;

    [CommandOption("--type <TYPE>")]
    [Description("Media type to search (Movie, Series, BoxSet, Person, MusicArtist, MusicAlbum, MusicVideo, Book, Trailer)")]
    public string MediaType { get; set; } = "Series";

    [CommandOption("--provider <NAME>")]
    [Description("Search a specific provider (e.g. TheMovieDb, AniDB, \"The Open Movie Database\")")]
    public string? Provider { get; set; }

    [CommandOption("--year <YEAR>")]
    [Description("Filter by production year")]
    public int? Year { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Term))
            return ValidationResult.Error("A search term is required.");

        var valid = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Movie", "Series", "BoxSet", "Person",
            "MusicArtist", "MusicAlbum", "MusicVideo", "Book", "Trailer",
        };

        if (!valid.Contains(MediaType))
            return ValidationResult.Error($"Invalid type '{MediaType}'. Must be one of: {string.Join(", ", valid)}");

        return ValidationResult.Success();
    }
}

public sealed class ItemsRemoteSearchCommand : ApiCommand<ItemsRemoteSearchSettings>
{
    public ItemsRemoteSearchCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ItemsRemoteSearchSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var results = await SearchAsync(client, settings, cancellationToken);

        if (results is null || results.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No results found.[/]");
            return 0;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(results.Select(r => new
            {
                name = r.Name,
                year = r.ProductionYear,
                providerIds = r.ProviderIds?.AdditionalData,
                provider = r.SearchProviderName,
                overview = r.Overview,
                imageUrl = r.ImageUrl,
            }));
            return 0;
        }

        var table = OutputHelper.CreateTable("Name", "Year", "Provider", "IDs");
        foreach (var r in results)
        {
            var ids = r.ProviderIds?.AdditionalData;
            var idStr = ids is not null
                ? string.Join(", ", ids.Select(kv => $"{kv.Key}={kv.Value}"))
                : "";

            table.AddRow(
                Markup.Escape(r.Name ?? "(untitled)"),
                r.ProductionYear?.ToString() ?? "",
                Markup.Escape(r.SearchProviderName ?? ""),
                Markup.Escape(idStr));
        }

        OutputHelper.WriteTable(table);
        AnsiConsole.MarkupLine($"[dim]{results.Count} results[/]");
        return 0;
    }

    private static async Task<List<RemoteSearchResult>?> SearchAsync(
        JellyfinApiClient client, ItemsRemoteSearchSettings settings, CancellationToken ct)
    {
        var type = settings.MediaType.ToLowerInvariant();
        var rs = client.Items.RemoteSearch;

        return type switch
        {
            "movie" => await rs.Movie.PostAsync(new MovieInfoRemoteSearchQuery
            {
                SearchInfo = new MovieInfo { Name = settings.Term, Year = settings.Year },
                SearchProviderName = settings.Provider,
            }, cancellationToken: ct),

            "series" => await rs.Series.PostAsync(new SeriesInfoRemoteSearchQuery
            {
                SearchInfo = new SeriesInfo { Name = settings.Term, Year = settings.Year },
                SearchProviderName = settings.Provider,
            }, cancellationToken: ct),

            "boxset" => await rs.BoxSet.PostAsync(new BoxSetInfoRemoteSearchQuery
            {
                SearchInfo = new BoxSetInfo { Name = settings.Term, Year = settings.Year },
                SearchProviderName = settings.Provider,
            }, cancellationToken: ct),

            "person" => await rs.Person.PostAsync(new PersonLookupInfoRemoteSearchQuery
            {
                SearchInfo = new PersonLookupInfo { Name = settings.Term, Year = settings.Year },
                SearchProviderName = settings.Provider,
            }, cancellationToken: ct),

            "musicartist" => await rs.MusicArtist.PostAsync(new ArtistInfoRemoteSearchQuery
            {
                SearchInfo = new ArtistInfo { Name = settings.Term, Year = settings.Year },
                SearchProviderName = settings.Provider,
            }, cancellationToken: ct),

            "musicalbum" => await rs.MusicAlbum.PostAsync(new AlbumInfoRemoteSearchQuery
            {
                SearchInfo = new AlbumInfo { Name = settings.Term, Year = settings.Year },
                SearchProviderName = settings.Provider,
            }, cancellationToken: ct),

            "musicvideo" => await rs.MusicVideo.PostAsync(new MusicVideoInfoRemoteSearchQuery
            {
                SearchInfo = new MusicVideoInfo { Name = settings.Term, Year = settings.Year },
                SearchProviderName = settings.Provider,
            }, cancellationToken: ct),

            "book" => await rs.Book.PostAsync(new BookInfoRemoteSearchQuery
            {
                SearchInfo = new BookInfo { Name = settings.Term, Year = settings.Year },
                SearchProviderName = settings.Provider,
            }, cancellationToken: ct),

            "trailer" => await rs.Trailer.PostAsync(new TrailerInfoRemoteSearchQuery
            {
                SearchInfo = new TrailerInfo { Name = settings.Term, Year = settings.Year },
                SearchProviderName = settings.Provider,
            }, cancellationToken: ct),

            _ => null,
        };
    }
}

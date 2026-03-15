using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Api.Generated.Models;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Items;

public sealed class ItemsThemesSettings : GlobalSettings
{
    [CommandArgument(0, "<ID>")]
    [Description("Item ID")]
    public string Id { get; set; } = string.Empty;

    [CommandOption("--inherit-from-parent")]
    [Description("Include theme media inherited from parent items")]
    public bool InheritFromParent { get; set; }

    public override ValidationResult Validate()
    {
        return Guid.TryParse(Id, out _)
            ? ValidationResult.Success()
            : ValidationResult.Error("A valid item ID (GUID) is required.");
    }
}

public sealed class ItemsThemesCommand : ApiCommand<ItemsThemesSettings>
{
    public ItemsThemesCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ItemsThemesSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var userId = await ResolveOptionalUserIdAsync(settings, client, cancellationToken);
        var result = await client.Items[Guid.Parse(settings.Id)].ThemeMedia.GetAsync(config =>
        {
            config.QueryParameters.UserId = userId;
            config.QueryParameters.InheritFromParent = settings.InheritFromParent ? true : null;
        }, cancellationToken);

        if (settings.Json)
        {
            OutputHelper.WriteJson(result ?? new AllThemeMediaResult());
            return 0;
        }

        var rows = FlattenThemeRows(result).ToList();
        if (rows.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No theme media found.[/]");
            return 0;
        }

        var table = OutputHelper.CreateTable("Category", "Id", "Name", "Type", "Runtime");
        foreach (var row in rows)
        {
            table.AddRow(
                row.Category,
                row.Item.Id?.ToString() ?? string.Empty,
                Markup.Escape(row.Item.Name ?? "(untitled)"),
                row.Item.Type?.ToString() ?? string.Empty,
                ItemsCommandOutputHelper.FormatRuntime(row.Item.RunTimeTicks));
        }

        OutputHelper.WriteTable(table);
        return 0;
    }

    private static IEnumerable<(string Category, BaseItemDto Item)> FlattenThemeRows(AllThemeMediaResult? result)
    {
        return EnumerateThemeRows("Theme Song", result?.ThemeSongsResult)
            .Concat(EnumerateThemeRows("Theme Video", result?.ThemeVideosResult))
            .Concat(EnumerateThemeRows("Soundtrack", result?.SoundtrackSongsResult));
    }

    private static IEnumerable<(string Category, BaseItemDto Item)> EnumerateThemeRows(string category, ThemeMediaResult? result)
    {
        foreach (var item in result?.Items ?? [])
            yield return (category, item);
    }
}

using Jellyfin.Cli.Api.Generated.Models;
using Jellyfin.Cli.Common;

using Spectre.Console;

namespace Jellyfin.Cli.Commands.Items;

internal static class ItemsCommandOutputHelper
{
    internal static string FormatTimestamp(long? ticks)
    {
        if (ticks is null or < 0)
            return string.Empty;

        var ts = TimeSpan.FromTicks(ticks.Value);
        return ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}"
            : $"{ts.Minutes:D2}:{ts.Seconds:D2}";
    }

    internal static string FormatRuntime(long? ticks)
    {
        if (ticks is null or 0)
            return string.Empty;

        var ts = TimeSpan.FromTicks(ticks.Value);
        return ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours}h {ts.Minutes:D2}m"
            : $"{ts.Minutes}m {ts.Seconds:D2}s";
    }

    internal static int WriteBaseItemQueryResult(
        GlobalSettings settings,
        BaseItemDtoQueryResult? result,
        string emptyMessage)
    {
        var items = result?.Items ?? [];
        if (items.Count == 0)
        {
            AnsiConsole.MarkupLine($"[yellow]{Markup.Escape(emptyMessage)}[/]");
            return 0;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(result);
            return 0;
        }

        var table = OutputHelper.CreateTable("Id", "Name", "Type", "Year", "Runtime");
        foreach (var item in items)
        {
            table.AddRow(
                item.Id?.ToString() ?? string.Empty,
                Markup.Escape(item.Name ?? "(untitled)"),
                item.Type?.ToString() ?? string.Empty,
                item.ProductionYear?.ToString() ?? string.Empty,
                FormatRuntime(item.RunTimeTicks));
        }

        OutputHelper.WriteTable(table);
        if (result?.TotalRecordCount is int totalRecordCount)
            AnsiConsole.MarkupLine($"[dim]Showing {items.Count} of {totalRecordCount} items[/]");

        return 0;
    }

    internal static string RenderLyrics(LyricDto lyric)
    {
        return string.Join(
            Environment.NewLine,
            (lyric.Lyrics ?? [])
                .Select(line => string.IsNullOrWhiteSpace(line.Text)
                    ? string.Empty
                    : line.Text)
                .Where(line => !string.IsNullOrWhiteSpace(line)));
    }

    internal static int WriteLyrics(GlobalSettings settings, LyricDto? lyric, string emptyMessage)
    {
        if (lyric is null)
        {
            AnsiConsole.MarkupLine($"[yellow]{Markup.Escape(emptyMessage)}[/]");
            return 0;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(lyric);
            return 0;
        }

        var metadata = lyric.Metadata;
        var hasMetadata =
            !string.IsNullOrWhiteSpace(metadata?.Title) ||
            !string.IsNullOrWhiteSpace(metadata?.Artist) ||
            !string.IsNullOrWhiteSpace(metadata?.Album) ||
            !string.IsNullOrWhiteSpace(metadata?.Author) ||
            metadata?.IsSynced is not null ||
            metadata?.Length is not null;

        if (hasMetadata)
        {
            var table = OutputHelper.CreateTable("Field", "Value");
            if (!string.IsNullOrWhiteSpace(metadata?.Title))
                table.AddRow("Title", Markup.Escape(metadata.Title));
            if (!string.IsNullOrWhiteSpace(metadata?.Artist))
                table.AddRow("Artist", Markup.Escape(metadata.Artist));
            if (!string.IsNullOrWhiteSpace(metadata?.Album))
                table.AddRow("Album", Markup.Escape(metadata.Album));
            if (!string.IsNullOrWhiteSpace(metadata?.Author))
                table.AddRow("Author", Markup.Escape(metadata.Author));
            if (metadata?.IsSynced is not null)
                table.AddRow("Synced", metadata.IsSynced.Value ? "Yes" : "No");
            if (metadata?.Length is not null)
                table.AddRow("Length", FormatRuntime(metadata.Length));
            OutputHelper.WriteTable(table);
        }

        var lines = lyric.Lyrics ?? [];
        if (lines.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No lyric lines found.[/]");
            return 0;
        }

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line.Text))
                continue;

            var prefix = metadata?.IsSynced == true && line.Start is not null
                ? $"[dim][{FormatTimestamp(line.Start)}][/]\u0020"
                : string.Empty;
            AnsiConsole.MarkupLine($"{prefix}{Markup.Escape(line.Text)}");
        }

        return 0;
    }
}

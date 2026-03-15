using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console;

namespace Jellyfin.Cli.Common;

public static class OutputHelper
{
    private const int MinConsoleWidth = 160;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static void WriteJson<T>(T data)
    {
        var json = JsonSerializer.Serialize(data, JsonOptions);
        AnsiConsole.WriteLine(json);
    }

    public static void WriteTable(Table table)
    {
        EnsureMinimumWidth();
        AnsiConsole.Write(table);
    }

    public static Table CreateTable(params string[] columns)
    {
        var table = new Table().BorderStyle(Style.Parse("dim"));
        foreach (var col in columns)
            table.AddColumn(new TableColumn(col).NoWrap());
        return table;
    }

    public static bool Confirm(string prompt, bool defaultValue = false)
    {
        return AnsiConsole.Confirm(prompt, defaultValue);
    }

    public static string FormatBytes(long? bytes)
    {
        if (bytes is null or < 0)
            return string.Empty;

        var value = (double)bytes.Value;
        var units = new[] { "B", "KB", "MB", "GB", "TB" };
        var unitIndex = 0;

        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return unitIndex == 0
            ? $"{value:0} {units[unitIndex]}"
            : $"{value:0.##} {units[unitIndex]}";
    }

    public static string FormatBitrate(long? bitsPerSecond)
    {
        if (bitsPerSecond is null or < 0)
            return string.Empty;

        return FormatBitrateValue(bitsPerSecond.Value);
    }

    public static string FormatBitrate(double bitsPerSecond)
    {
        return FormatBitrateValue(bitsPerSecond);
    }

    private static string FormatBitrateValue(double bitsPerSecond)
    {
        if (bitsPerSecond < 0)
            return string.Empty;

        if (bitsPerSecond >= 1_000_000)
            return $"{bitsPerSecond / 1_000_000:0.##} Mbps";

        if (bitsPerSecond >= 1_000)
            return $"{bitsPerSecond / 1_000:0.##} Kbps";

        return $"{bitsPerSecond:0} bps";
    }

    public static string FormatDuration(TimeSpan duration)
    {
        return duration.TotalHours >= 1
            ? $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}"
            : $"{duration.Minutes:D2}:{duration.Seconds:D2}";
    }

    public static string FormatTicks(long? ticks)
    {
        if (ticks is null or < 0)
            return string.Empty;

        return FormatDuration(TimeSpan.FromTicks(ticks.Value));
    }

    /// <summary>
    /// Ensures AnsiConsole.Profile.Width is at least <see cref="MinConsoleWidth"/>
    /// so that tables don't collapse to a single character when the terminal width
    /// cannot be detected (e.g. piped output, non-interactive shells).
    /// </summary>
    private static void EnsureMinimumWidth()
    {
        if (AnsiConsole.Profile.Width < MinConsoleWidth)
            AnsiConsole.Profile.Width = MinConsoleWidth;
    }
}

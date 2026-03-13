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

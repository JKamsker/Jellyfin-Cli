using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console;

namespace Jellyfin.Cli.Common;

public static class OutputHelper
{
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
}

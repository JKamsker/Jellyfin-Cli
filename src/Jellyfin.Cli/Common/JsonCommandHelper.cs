using System.Text.Json;

namespace Jellyfin.Cli.Common;

internal static class JsonCommandHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    internal static T DeserializeFromFileOrInline<T>(string? filePath, string? inlineJson, string argumentName)
    {
        var json = ReadTextFromFileOrInline(filePath, inlineJson, argumentName);
        var value = JsonSerializer.Deserialize<T>(json, JsonOptions);

        if (value is null)
            throw new InvalidOperationException($"The JSON supplied via {argumentName} was empty.");

        return value;
    }

    internal static string ReadTextFromFileOrInline(string? filePath, string? inlineText, string argumentName)
    {
        var hasFile = !string.IsNullOrWhiteSpace(filePath);
        var hasInline = !string.IsNullOrWhiteSpace(inlineText);

        if (hasFile == hasInline)
            throw new InvalidOperationException($"Specify exactly one of {argumentName} file or inline text.");

        if (hasFile)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Input file not found.", filePath);

            return File.ReadAllText(filePath);
        }

        return inlineText!;
    }
}

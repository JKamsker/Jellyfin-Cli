using System.Text;
using System.Text.Json;

using Spectre.Console;

namespace Jellyfin.Cli.Common;

internal static class ResponseOutputHelper
{
    internal static async Task<string> ReadStreamAsStringAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    internal static void WriteJsonOrText(string content)
    {
        try
        {
            using var document = JsonDocument.Parse(content);
            OutputHelper.WriteJson(document.RootElement);
        }
        catch (JsonException)
        {
            AnsiConsole.WriteLine(content);
        }
    }
}

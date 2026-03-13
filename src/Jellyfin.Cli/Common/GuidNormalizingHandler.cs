using System.Text;
using System.Text.RegularExpressions;

namespace Jellyfin.Cli.Common;

/// <summary>
/// HTTP message handler that normalizes Jellyfin's dashless GUID format
/// (e.g., "f692394d1ad442e696e164f83406542d") to the standard dashed format
/// ("f692394d-1ad4-42e6-96e1-64f83406542d") so Kiota's JSON deserializer can parse them.
/// </summary>
public sealed partial class GuidNormalizingHandler : DelegatingHandler
{
    // Matches a JSON string value that is exactly 32 hex chars (a dashless GUID)
    [GeneratedRegex("\"([0-9a-fA-F]{32})\"")]
    private static partial Regex DashlessGuidPattern();

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.Content.Headers.ContentType?.MediaType is not "application/json")
            return response;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        var normalized = DashlessGuidPattern().Replace(body, match =>
        {
            var hex = match.Groups[1].Value;
            // Format as 8-4-4-4-12
            return $"\"{hex[..8]}-{hex[8..12]}-{hex[12..16]}-{hex[16..20]}-{hex[20..]}\"";
        });

        if (!ReferenceEquals(body, normalized))
        {
            response.Content = new StringContent(normalized, Encoding.UTF8,
                response.Content.Headers.ContentType);
        }

        return response;
    }
}

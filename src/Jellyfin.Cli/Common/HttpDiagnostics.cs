using System.Collections;
using System.Net.Http.Headers;
using System.Text;

using Microsoft.Kiota.Abstractions;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Common;

internal sealed record HttpExchangeInfo(
    DateTimeOffset Timestamp,
    string Method,
    string? Url,
    string? RequestHeaders,
    string? RequestBody,
    int? StatusCode,
    string? ReasonPhrase,
    string? ResponseHeaders,
    string? ResponseBody,
    string? TransportError);

internal static class HttpDiagnosticsContext
{
    private static readonly object Sync = new();
    private static HttpExchangeInfo? _lastExchange;

    internal static HttpExchangeInfo? LastExchange
    {
        get
        {
            lock (Sync)
                return _lastExchange;
        }
        set
        {
            lock (Sync)
                _lastExchange = value;
        }
    }

    internal static void Clear()
    {
        lock (Sync)
            _lastExchange = null;
    }
}

internal sealed class DiagnosticLoggingHandler : DelegatingHandler
{
    private const int MaxBodyBytes = 64 * 1024;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string? requestBody = null;
        string? requestHeaders = null;

        try
        {
            requestHeaders = FormatHeaders(request.Headers, request.Content?.Headers);
            requestBody = await ReadContentPreviewAsync(request.Content, cancellationToken);

            var response = await base.SendAsync(request, cancellationToken);
            var responseHeaders = FormatHeaders(response.Headers, response.Content?.Headers);
            var responseBody = await SnapshotResponseBodyAsync(response, cancellationToken);

            HttpDiagnosticsContext.LastExchange = new HttpExchangeInfo(
                DateTimeOffset.UtcNow,
                request.Method.Method,
                request.RequestUri?.ToString(),
                requestHeaders,
                requestBody,
                (int)response.StatusCode,
                response.ReasonPhrase,
                responseHeaders,
                responseBody,
                null);

            return response;
        }
        catch (Exception ex)
        {
            HttpDiagnosticsContext.LastExchange = new HttpExchangeInfo(
                DateTimeOffset.UtcNow,
                request.Method.Method,
                request.RequestUri?.ToString(),
                requestHeaders,
                requestBody,
                null,
                null,
                null,
                null,
                ex.ToString());

            throw;
        }
    }

    private static async Task<string?> SnapshotResponseBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.Content is null)
            return null;

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var preview = FormatContentPreview(bytes, response.Content.Headers.ContentType?.MediaType);

        var replacement = new ByteArrayContent(bytes);
        foreach (var header in response.Content.Headers)
            replacement.Headers.TryAddWithoutValidation(header.Key, header.Value);

        response.Content.Dispose();
        response.Content = replacement;

        return preview;
    }

    private static async Task<string?> ReadContentPreviewAsync(HttpContent? content, CancellationToken cancellationToken)
    {
        if (content is null)
            return null;

        await content.LoadIntoBufferAsync();
        var bytes = await content.ReadAsByteArrayAsync(cancellationToken);
        return FormatContentPreview(bytes, content.Headers.ContentType?.MediaType);
    }

    private static string? FormatHeaders(HttpHeaders? primaryHeaders, HttpHeaders? contentHeaders)
    {
        var entries = new List<string>();

        if (primaryHeaders is not null)
        {
            foreach (var header in primaryHeaders)
                entries.Add($"{header.Key}: {string.Join(", ", header.Value)}");
        }

        if (contentHeaders is not null)
        {
            foreach (var header in contentHeaders)
                entries.Add($"{header.Key}: {string.Join(", ", header.Value)}");
        }

        return entries.Count == 0 ? null : string.Join(Environment.NewLine, entries);
    }

    private static string FormatContentPreview(byte[] bytes, string? mediaType)
    {
        var truncated = bytes.Length > MaxBodyBytes;
        var effectiveBytes = truncated ? bytes[..MaxBodyBytes] : bytes;

        if (!LooksLikeText(mediaType, effectiveBytes))
            return $"<{mediaType ?? "binary"}; {bytes.Length} bytes>";

        var text = Encoding.UTF8.GetString(effectiveBytes);
        return truncated ? text + $"{Environment.NewLine}[truncated after {MaxBodyBytes} bytes]" : text;
    }

    private static bool LooksLikeText(string? mediaType, byte[] bytes)
    {
        if (!string.IsNullOrWhiteSpace(mediaType))
        {
            if (mediaType.StartsWith("text/", StringComparison.OrdinalIgnoreCase))
                return true;

            if (mediaType.Contains("json", StringComparison.OrdinalIgnoreCase) ||
                mediaType.Contains("xml", StringComparison.OrdinalIgnoreCase) ||
                mediaType.Contains("javascript", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return bytes.Take(256).All(b => b is 9 or 10 or 13 || b >= 32);
    }
}

internal static class CliErrorReporter
{
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "jf",
        "logs");

    internal static void ReportApiException(CommandContext context, GlobalSettings settings, ApiException ex)
    {
        var logPath = WriteDiagnosticLog(context, settings, ex);
        var exchange = HttpDiagnosticsContext.LastExchange;
        var reason = ex.ResponseStatusCode switch
        {
            401 => "Authentication required. Run 'jf auth login' first.",
            403 => "Permission denied. This may require admin privileges.",
            404 => "Resource not found.",
            _ => ex.Message,
        };

        Spectre.Console.AnsiConsole.MarkupLine($"[red]API error {ex.ResponseStatusCode}:[/] {Spectre.Console.Markup.Escape(reason)}");
        WriteExchangeSummary(exchange);
        Spectre.Console.AnsiConsole.MarkupLine($"[yellow]Diagnostic log:[/] {Spectre.Console.Markup.Escape(logPath)}");
        Spectre.Console.AnsiConsole.MarkupLine("[dim]Include that log file when reporting this issue.[/]");
    }

    internal static void ReportNetworkException(CommandContext context, GlobalSettings settings, HttpRequestException ex)
    {
        var logPath = WriteDiagnosticLog(context, settings, ex);
        Spectre.Console.AnsiConsole.MarkupLine($"[red]Network error:[/] {Spectre.Console.Markup.Escape(ex.Message)}");
        WriteExchangeSummary(HttpDiagnosticsContext.LastExchange);
        Spectre.Console.AnsiConsole.MarkupLine($"[yellow]Diagnostic log:[/] {Spectre.Console.Markup.Escape(logPath)}");
    }

    internal static void ReportHttpFailure(CommandContext context, GlobalSettings settings, int statusCode, string? reasonPhrase, string? extraDetails = null)
    {
        var exception = new InvalidOperationException($"HTTP {statusCode} {reasonPhrase}".Trim());
        var logPath = WriteDiagnosticLog(context, settings, exception, extraDetails);

        Spectre.Console.AnsiConsole.MarkupLine($"[red]HTTP error {statusCode}:[/] {Spectre.Console.Markup.Escape(reasonPhrase ?? "Request failed.")}");
        WriteExchangeSummary(HttpDiagnosticsContext.LastExchange);
        Spectre.Console.AnsiConsole.MarkupLine($"[yellow]Diagnostic log:[/] {Spectre.Console.Markup.Escape(logPath)}");
        Spectre.Console.AnsiConsole.MarkupLine("[dim]Include that log file when reporting this issue.[/]");
    }

    internal static void ReportUnexpectedException(CommandContext context, GlobalSettings settings, Exception ex, string? headline = null)
    {
        var logPath = WriteDiagnosticLog(context, settings, ex);
        var title = string.IsNullOrWhiteSpace(headline)
            ? "Unexpected client error while processing the server response."
            : headline;

        Spectre.Console.AnsiConsole.MarkupLine($"[red]{Spectre.Console.Markup.Escape(title)}[/]");
        Spectre.Console.AnsiConsole.MarkupLine($"[red]{Spectre.Console.Markup.Escape(ex.GetType().Name)}:[/] {Spectre.Console.Markup.Escape(ex.Message)}");
        WriteExchangeSummary(HttpDiagnosticsContext.LastExchange);
        Spectre.Console.AnsiConsole.MarkupLine($"[yellow]Diagnostic log:[/] {Spectre.Console.Markup.Escape(logPath)}");
        Spectre.Console.AnsiConsole.MarkupLine("[dim]Include that log file and the triggering command when reporting this issue.[/]");
    }

    internal static string WriteDiagnosticLog(CommandContext context, GlobalSettings settings, Exception exception, string? extraDetails = null)
    {
        Directory.CreateDirectory(LogDirectory);
        var timestamp = DateTimeOffset.Now;
        var filePath = Path.Combine(LogDirectory, $"jf-error-{timestamp:yyyyMMdd-HHmmss-fff}.log");
        var exchange = HttpDiagnosticsContext.LastExchange;

        var builder = new StringBuilder();
        builder.AppendLine($"Timestamp: {timestamp:O}");
        builder.AppendLine($"Command: {Environment.CommandLine}");
        builder.AppendLine($"CommandName: {context.Name}");
        builder.AppendLine($"Verbose: {settings.Verbose}");
        builder.AppendLine($"JSON: {settings.Json}");
        builder.AppendLine();
        builder.AppendLine($"ExceptionType: {exception.GetType().FullName}");
        builder.AppendLine($"ExceptionMessage: {exception.Message}");

        if (exception is ApiException apiException)
        {
            builder.AppendLine($"ResponseStatusCode: {apiException.ResponseStatusCode}");
            if (apiException.ResponseHeaders is not null)
                builder.AppendLine($"ResponseHeadersFromException: {FormatHeadersDictionary(apiException.ResponseHeaders)}");
        }

        if (!string.IsNullOrWhiteSpace(extraDetails))
        {
            builder.AppendLine();
            builder.AppendLine("ExtraDetails:");
            builder.AppendLine(extraDetails);
        }

        if (exchange is not null)
        {
            builder.AppendLine();
            builder.AppendLine("LastHttpExchange:");
            builder.AppendLine($"TimestampUtc: {exchange.Timestamp:O}");
            builder.AppendLine($"Request: {exchange.Method} {exchange.Url}");

            if (!string.IsNullOrWhiteSpace(exchange.RequestHeaders))
            {
                builder.AppendLine("RequestHeaders:");
                builder.AppendLine(exchange.RequestHeaders);
            }

            if (!string.IsNullOrWhiteSpace(exchange.RequestBody))
            {
                builder.AppendLine("RequestBody:");
                builder.AppendLine(exchange.RequestBody);
            }

            if (exchange.StatusCode is not null)
                builder.AppendLine($"ResponseStatus: {exchange.StatusCode} {exchange.ReasonPhrase}");

            if (!string.IsNullOrWhiteSpace(exchange.ResponseHeaders))
            {
                builder.AppendLine("ResponseHeaders:");
                builder.AppendLine(exchange.ResponseHeaders);
            }

            if (!string.IsNullOrWhiteSpace(exchange.ResponseBody))
            {
                builder.AppendLine("ResponseBody:");
                builder.AppendLine(exchange.ResponseBody);
            }

            if (!string.IsNullOrWhiteSpace(exchange.TransportError))
            {
                builder.AppendLine("TransportError:");
                builder.AppendLine(exchange.TransportError);
            }
        }

        builder.AppendLine();
        builder.AppendLine("StackTrace:");
        builder.AppendLine(exception.ToString());

        File.WriteAllText(filePath, builder.ToString());
        return filePath;
    }

    private static void WriteExchangeSummary(HttpExchangeInfo? exchange)
    {
        if (exchange is null)
            return;

        if (!string.IsNullOrWhiteSpace(exchange.Url))
            Spectre.Console.AnsiConsole.MarkupLine($"[dim]Request:[/] {Spectre.Console.Markup.Escape($"{exchange.Method} {exchange.Url}")}");

        if (exchange.StatusCode is not null)
            Spectre.Console.AnsiConsole.MarkupLine($"[dim]Response:[/] {exchange.StatusCode} {Spectre.Console.Markup.Escape(exchange.ReasonPhrase ?? string.Empty)}");

        if (!string.IsNullOrWhiteSpace(exchange.ResponseBody))
        {
            Spectre.Console.AnsiConsole.MarkupLine("[dim]Response body excerpt:[/]");
            Spectre.Console.AnsiConsole.WriteLine(TrimForConsole(exchange.ResponseBody, 4000));
        }

        if (!string.IsNullOrWhiteSpace(exchange.TransportError))
        {
            Spectre.Console.AnsiConsole.MarkupLine("[dim]Transport error:[/]");
            Spectre.Console.AnsiConsole.WriteLine(TrimForConsole(exchange.TransportError, 600));
        }
    }

    private static string TrimForConsole(string text, int maxChars)
    {
        return text.Length <= maxChars
            ? text
            : text[..maxChars] + $"{Environment.NewLine}[truncated in console output; see diagnostic log for full details]";
    }

    private static string FormatHeadersDictionary(IEnumerable<KeyValuePair<string, IEnumerable<string>>> dictionary)
    {
        var parts = new List<string>();
        foreach (var entry in dictionary)
            parts.Add($"{entry.Key}={string.Join(", ", entry.Value)}");
        return string.Join("; ", parts);
    }
}

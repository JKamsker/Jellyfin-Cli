using System.ComponentModel;
using System.Diagnostics;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Server;

public sealed class ServerBitrateTestSettings : GlobalSettings
{
    [CommandOption("--size <BYTES>")]
    [Description("Payload size to request from the server")]
    public int? Size { get; set; }

    public override ValidationResult Validate()
    {
        return Size is null or > 0
            ? ValidationResult.Success()
            : ValidationResult.Error("--size must be greater than zero.");
    }
}

public sealed class ServerBitrateTestCommand : ApiCommand<ServerBitrateTestSettings>
{
    public ServerBitrateTestCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ServerBitrateTestSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var requestedSize = settings.Size ?? 102_400;
        var stopwatch = Stopwatch.StartNew();
        long bytesRead = 0;

        await using var stream = await client.Playback.BitrateTest.GetAsync(config =>
        {
            config.QueryParameters.Size = requestedSize;
        }, cancellationToken);

        if (stream is null)
        {
            AnsiConsole.MarkupLine("[yellow]No bitrate test payload returned.[/]");
            return 0;
        }

        var buffer = new byte[81_920];
        while (true)
        {
            var read = await stream.ReadAsync(buffer, cancellationToken);
            if (read == 0)
                break;

            bytesRead += read;
        }

        stopwatch.Stop();
        var elapsed = stopwatch.Elapsed;
        var bitsPerSecond = elapsed.TotalSeconds > 0
            ? bytesRead * 8d / elapsed.TotalSeconds
            : 0d;

        if (settings.Json)
        {
            OutputHelper.WriteJson(new
            {
                requestedBytes = requestedSize,
                receivedBytes = bytesRead,
                elapsedMilliseconds = elapsed.TotalMilliseconds,
                bitsPerSecond,
                megabitsPerSecond = bitsPerSecond / 1_000_000d,
            });
            return 0;
        }

        var table = OutputHelper.CreateTable("Field", "Value");
        table.AddRow("Requested", OutputHelper.FormatBytes(requestedSize));
        table.AddRow("Received", OutputHelper.FormatBytes(bytesRead));
        table.AddRow("Duration", OutputHelper.FormatDuration(elapsed));
        table.AddRow("Throughput", OutputHelper.FormatBitrate(bitsPerSecond));
        OutputHelper.WriteTable(table);
        return 0;
    }
}

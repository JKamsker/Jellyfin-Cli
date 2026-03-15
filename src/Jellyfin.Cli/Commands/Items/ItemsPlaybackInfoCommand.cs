using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Api.Generated.Models;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Items;

public sealed class ItemsPlaybackInfoSettings : GlobalSettings
{
    [CommandArgument(0, "<ID>")]
    [Description("Item ID")]
    public string Id { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        return Guid.TryParse(Id, out _)
            ? ValidationResult.Success()
            : ValidationResult.Error("A valid item ID (GUID) is required.");
    }
}

public sealed class ItemsPlaybackInfoCommand : ApiCommand<ItemsPlaybackInfoSettings>
{
    public ItemsPlaybackInfoCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ItemsPlaybackInfoSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var userId = await ResolveOptionalUserIdAsync(settings, client, cancellationToken);
        var result = await client.Items[Guid.Parse(settings.Id)].PlaybackInfo.GetAsync(config =>
        {
            config.QueryParameters.UserId = userId;
        }, cancellationToken);

        if (result is null)
        {
            AnsiConsole.MarkupLine("[yellow]No playback info returned.[/]");
            return 0;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(result);
            return 0;
        }

        if (!string.IsNullOrWhiteSpace(result.PlaySessionId) || result.ErrorCode is not null)
        {
            var metadataTable = OutputHelper.CreateTable("Field", "Value");
            if (!string.IsNullOrWhiteSpace(result.PlaySessionId))
                metadataTable.AddRow("Play Session", result.PlaySessionId);
            if (result.ErrorCode is not null)
                metadataTable.AddRow("Error Code", result.ErrorCode.ToString() ?? string.Empty);
            OutputHelper.WriteTable(metadataTable);
        }

        var sources = result.MediaSources ?? [];
        if (sources.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No media sources found.[/]");
            return 0;
        }

        var sourceTable = OutputHelper.CreateTable(
            "SourceId", "Name", "Type", "Container", "Bitrate", "Size", "DirectPlay", "DirectStream", "Transcode", "Path");

        foreach (var source in sources)
        {
            sourceTable.AddRow(
                source.Id ?? string.Empty,
                Markup.Escape(source.Name ?? "(unnamed)"),
                source.Type?.ToString() ?? string.Empty,
                source.Container ?? string.Empty,
                OutputHelper.FormatBitrate(source.Bitrate),
                OutputHelper.FormatBytes(source.Size),
                FormatSupport(source.SupportsDirectPlay),
                FormatSupport(source.SupportsDirectStream),
                FormatSupport(source.SupportsTranscoding),
                Markup.Escape(source.Path ?? string.Empty));
        }

        OutputHelper.WriteTable(sourceTable);

        var streams = sources
            .SelectMany(source => (source.MediaStreams ?? []).Select(stream => (Source: source, Stream: stream)))
            .OrderBy(entry => entry.Source.Id)
            .ThenBy(entry => entry.Stream.Index ?? int.MaxValue)
            .ToList();

        if (streams.Count == 0)
            return 0;

        var streamTable = OutputHelper.CreateTable("SourceId", "Index", "Type", "Codec", "Language", "Details", "Title", "Flags");
        foreach (var (source, stream) in streams)
        {
            streamTable.AddRow(
                source.Id ?? string.Empty,
                stream.Index?.ToString() ?? string.Empty,
                stream.Type?.ToString() ?? string.Empty,
                stream.Codec ?? string.Empty,
                stream.Language ?? string.Empty,
                BuildStreamDetails(stream),
                Markup.Escape(stream.DisplayTitle ?? stream.Title ?? string.Empty),
                BuildStreamFlags(stream));
        }

        OutputHelper.WriteTable(streamTable);
        return 0;
    }

    private static string FormatSupport(bool? value)
    {
        return value switch
        {
            true => "Yes",
            false => "No",
            _ => string.Empty,
        };
    }

    private static string BuildStreamDetails(MediaStream stream)
    {
        return stream.Type switch
        {
            MediaStream_Type.Video => string.Join(' ', new[]
            {
                stream.Width is not null && stream.Height is not null ? $"{stream.Width}x{stream.Height}" : null,
                stream.AverageFrameRate is not null ? $"{stream.AverageFrameRate:0.##} fps" : null,
                stream.BitRate is not null ? OutputHelper.FormatBitrate((long?)stream.BitRate.Value) : null,
            }.Where(value => !string.IsNullOrWhiteSpace(value))),
            MediaStream_Type.Audio => string.Join(' ', new[]
            {
                stream.Channels is not null ? $"{stream.Channels} ch" : null,
                stream.ChannelLayout,
                stream.SampleRate is not null ? $"{stream.SampleRate} Hz" : null,
                stream.BitRate is not null ? OutputHelper.FormatBitrate((long?)stream.BitRate.Value) : null,
            }.Where(value => !string.IsNullOrWhiteSpace(value))),
            MediaStream_Type.Subtitle => stream.IsTextSubtitleStream == true ? "Text subtitle" : "Image subtitle",
            _ => string.Empty,
        };
    }

    private static string BuildStreamFlags(MediaStream stream)
    {
        var flags = new List<string>();
        if (stream.IsDefault == true)
            flags.Add("default");
        if (stream.IsForced == true)
            flags.Add("forced");
        if (stream.IsExternal == true)
            flags.Add("external");
        if (stream.IsHearingImpaired == true)
            flags.Add("hearing-impaired");

        return string.Join(", ", flags);
    }
}

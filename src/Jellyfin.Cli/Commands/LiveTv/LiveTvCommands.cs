using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Microsoft.Kiota.Abstractions;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.LiveTv;

// ---------------------------------------------------------------------------
// Live TV Info
// ---------------------------------------------------------------------------

public sealed class LiveTvInfoCommand : ApiCommand<GlobalSettings>
{
    public LiveTvInfoCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, GlobalSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var info = await client.LiveTv.Info.GetAsync();

        if (info is null)
        {
            AnsiConsole.MarkupLine("[red]Could not retrieve Live TV info.[/]");
            return 1;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(new
            {
                isEnabled = info.IsEnabled,
                enabledUsers = info.EnabledUsers,
                services = info.Services?.Select(s => new
                {
                    name = s.Name,
                    status = s.Status?.ToString(),
                    version = s.Version,
                }),
            });
            return 0;
        }

        AnsiConsole.MarkupLine($"[bold]Enabled:[/] {info.IsEnabled}");
        AnsiConsole.MarkupLine($"[bold]Enabled Users:[/] {info.EnabledUsers?.Count ?? 0}");

        if (info.Services is { Count: > 0 })
        {
            var table = OutputHelper.CreateTable("Service", "Status", "Version");
            foreach (var svc in info.Services)
            {
                table.AddRow(
                    svc.Name ?? "",
                    svc.Status?.ToString() ?? "",
                    svc.Version ?? "");
            }

            OutputHelper.WriteTable(table);
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]No Live TV services configured.[/]");
        }

        return 0;
    }
}

// ---------------------------------------------------------------------------
// List channels
// ---------------------------------------------------------------------------

public sealed class LiveTvChannelsCommand : ApiCommand<GlobalSettings>
{
    public LiveTvChannelsCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, GlobalSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var result = await client.LiveTv.Channels.GetAsync(config =>
        {
            config.QueryParameters.Limit = settings.Limit;
            config.QueryParameters.StartIndex = settings.Start;
        });

        var items = result?.Items;
        if (items is null || items.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No channels found.[/]");
            return 0;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(items.Select(c => new
            {
                id = c.Id,
                name = c.Name,
                number = c.ChannelNumber,
                type = c.ChannelType?.ToString(),
            }));
            return 0;
        }

        var table = OutputHelper.CreateTable("Id", "Name", "Number", "Type");
        foreach (var c in items)
        {
            table.AddRow(
                c.Id?.ToString() ?? "",
                c.Name ?? "",
                c.ChannelNumber ?? "",
                c.ChannelType?.ToString() ?? "");
        }

        OutputHelper.WriteTable(table);

        if (result?.TotalRecordCount is not null)
        {
            AnsiConsole.MarkupLine(
                $"[dim]Showing {items.Count} of {result.TotalRecordCount} total channels.[/]");
        }

        return 0;
    }
}

// ---------------------------------------------------------------------------
// Guide / Programs
// ---------------------------------------------------------------------------

public sealed class LiveTvGuideCommand : ApiCommand<GlobalSettings>
{
    public LiveTvGuideCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, GlobalSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var result = await client.LiveTv.Programs.GetAsync(config =>
        {
            config.QueryParameters.Limit = settings.Limit;
            config.QueryParameters.StartIndex = settings.Start;
        });

        var items = result?.Items;
        if (items is null || items.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No programs found.[/]");
            return 0;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(items.Select(p => new
            {
                id = p.Id,
                name = p.Name,
                channelName = p.ChannelName,
                startDate = p.StartDate,
                endDate = p.EndDate,
            }));
            return 0;
        }

        var table = OutputHelper.CreateTable("Id", "Name", "Channel", "Start", "End");
        foreach (var p in items)
        {
            table.AddRow(
                p.Id?.ToString() ?? "",
                p.Name ?? "",
                p.ChannelName ?? "",
                p.StartDate?.ToString("g") ?? "",
                p.EndDate?.ToString("g") ?? "");
        }

        OutputHelper.WriteTable(table);

        if (result?.TotalRecordCount is not null)
        {
            AnsiConsole.MarkupLine(
                $"[dim]Showing {items.Count} of {result.TotalRecordCount} total programs.[/]");
        }

        return 0;
    }
}

// ---------------------------------------------------------------------------
// Recordings
// ---------------------------------------------------------------------------

public sealed class LiveTvRecordingsCommand : ApiCommand<GlobalSettings>
{
    public LiveTvRecordingsCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, GlobalSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var result = await client.LiveTv.Recordings.GetAsync(config =>
        {
            config.QueryParameters.Limit = settings.Limit;
            config.QueryParameters.StartIndex = settings.Start;
        });

        var items = result?.Items;
        if (items is null || items.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No recordings found.[/]");
            return 0;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(items.Select(r => new
            {
                id = r.Id,
                name = r.Name,
                channelName = r.ChannelName,
                startDate = r.StartDate,
                status = r.Status?.ToString(),
            }));
            return 0;
        }

        var table = OutputHelper.CreateTable("Id", "Name", "Channel", "Start", "Status");
        foreach (var r in items)
        {
            table.AddRow(
                r.Id?.ToString() ?? "",
                r.Name ?? "",
                r.ChannelName ?? "",
                r.StartDate?.ToString("g") ?? "",
                r.Status?.ToString() ?? "");
        }

        OutputHelper.WriteTable(table);

        if (result?.TotalRecordCount is not null)
        {
            AnsiConsole.MarkupLine(
                $"[dim]Showing {items.Count} of {result.TotalRecordCount} total recordings.[/]");
        }

        return 0;
    }
}

// ---------------------------------------------------------------------------
// Timers
// ---------------------------------------------------------------------------

public sealed class LiveTvTimersCommand : ApiCommand<GlobalSettings>
{
    public LiveTvTimersCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, GlobalSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var result = await client.LiveTv.Timers.GetAsync();

        var items = result?.Items;
        if (items is null || items.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No timers found.[/]");
            return 0;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(items.Select(t => new
            {
                id = t.Id,
                name = t.Name,
                channelName = t.ChannelName,
                startDate = t.StartDate,
                endDate = t.EndDate,
                status = t.Status?.ToString(),
            }));
            return 0;
        }

        var table = OutputHelper.CreateTable("Id", "Name", "Channel", "Start", "End", "Status");
        foreach (var t in items)
        {
            table.AddRow(
                t.Id ?? "",
                t.Name ?? "",
                t.ChannelName ?? "",
                t.StartDate?.ToString("g") ?? "",
                t.EndDate?.ToString("g") ?? "",
                t.Status?.ToString() ?? "");
        }

        OutputHelper.WriteTable(table);
        return 0;
    }
}

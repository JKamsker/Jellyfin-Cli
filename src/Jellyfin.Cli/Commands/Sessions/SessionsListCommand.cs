using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Microsoft.Kiota.Abstractions;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Sessions;

public sealed class SessionsListSettings : GlobalSettings
{
    [CommandOption("--active-within <SECONDS>")]
    [Description("Only sessions active in the last N seconds")]
    public int? ActiveWithin { get; set; }

    [CommandOption("--device-id <ID>")]
    [Description("Filter by device id")]
    public string? DeviceId { get; set; }

    [CommandOption("--controllable-by <USER_ID>")]
    [Description("Filter by sessions controllable by this user id")]
    public Guid? ControllableBy { get; set; }
}

public sealed class SessionsListCommand : ApiCommand<SessionsListSettings>
{
    public SessionsListCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, SessionsListSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var sessions = await client.Sessions.GetAsync(config =>
        {
            config.QueryParameters.ActiveWithinSeconds = settings.ActiveWithin;
            config.QueryParameters.DeviceId = settings.DeviceId;
            config.QueryParameters.ControllableByUserId = settings.ControllableBy;
        });

        if (sessions is null || sessions.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No sessions found.[/]");
            return 0;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(sessions.Select(s => new
            {
                id = s.Id,
                userName = s.UserName,
                client = s.Client,
                deviceName = s.DeviceName,
                nowPlaying = s.NowPlayingItem?.Name,
                lastActive = s.LastActivityDate,
            }));
            return 0;
        }

        var table = OutputHelper.CreateTable("Id", "UserName", "Client", "DeviceName", "NowPlaying", "LastActive");
        foreach (var s in sessions)
        {
            table.AddRow(
                s.Id ?? "",
                s.UserName ?? "",
                s.Client ?? "",
                s.DeviceName ?? "",
                s.NowPlayingItem?.Name ?? "",
                s.LastActivityDate?.ToString("g") ?? "");
        }

        OutputHelper.WriteTable(table);
        return 0;
    }
}

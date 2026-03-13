using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Api.Generated.Models;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.SyncPlay;

// ---------------------------------------------------------------------------
// List SyncPlay groups
// ---------------------------------------------------------------------------

public sealed class SyncPlayListCommand : ApiCommand<GlobalSettings>
{
    public SyncPlayListCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, GlobalSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var groups = await client.SyncPlay.List.GetAsync();

        if (groups is null || groups.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No SyncPlay groups found.[/]");
            return 0;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(groups.Select(g => new
            {
                groupId = g.GroupId,
                groupName = g.GroupName,
                state = g.State?.ToString(),
                participants = g.Participants,
                lastUpdated = g.LastUpdatedAt,
            }));
            return 0;
        }

        var table = OutputHelper.CreateTable("GroupId", "Name", "State", "Participants", "LastUpdated");
        foreach (var g in groups)
        {
            var participantCount = g.Participants?.Count.ToString() ?? "0";

            table.AddRow(
                g.GroupId?.ToString() ?? "",
                g.GroupName ?? "",
                g.State?.ToString() ?? "",
                participantCount,
                g.LastUpdatedAt?.ToString("g") ?? "");
        }

        OutputHelper.WriteTable(table);
        return 0;
    }
}

// ---------------------------------------------------------------------------
// Create a SyncPlay group
// ---------------------------------------------------------------------------

public sealed class SyncPlayCreateSettings : GlobalSettings
{
    [CommandOption("--name <NAME>")]
    [Description("Name for the new SyncPlay group")]
    public string GroupName { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(GroupName))
            return ValidationResult.Error("--name is required.");

        return ValidationResult.Success();
    }
}

public sealed class SyncPlayCreateCommand : ApiCommand<SyncPlayCreateSettings>
{
    public SyncPlayCreateCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, SyncPlayCreateSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var body = new NewGroupRequestDto
        {
            GroupName = settings.GroupName,
        };

        var result = await client.SyncPlay.New.PostAsync(body);

        if (result is not null)
        {
            AnsiConsole.MarkupLine(
                $"[green]SyncPlay group created: [white]{result.GroupName}[/] ({result.GroupId})[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[green]SyncPlay group created.[/]");
        }

        return 0;
    }
}

// ---------------------------------------------------------------------------
// Join a SyncPlay group
// ---------------------------------------------------------------------------

public sealed class SyncPlayJoinSettings : GlobalSettings
{
    [CommandOption("--group-id <ID>")]
    [Description("Group ID (GUID) to join")]
    public string GroupId { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(GroupId))
            return ValidationResult.Error("--group-id is required.");

        if (!Guid.TryParse(GroupId, out _))
            return ValidationResult.Error("--group-id must be a valid GUID.");

        return ValidationResult.Success();
    }
}

public sealed class SyncPlayJoinCommand : ApiCommand<SyncPlayJoinSettings>
{
    public SyncPlayJoinCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, SyncPlayJoinSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var body = new JoinGroupRequestDto
        {
            GroupId = Guid.Parse(settings.GroupId),
        };

        await client.SyncPlay.Join.PostAsync(body);

        AnsiConsole.MarkupLine(
            $"[green]Joined SyncPlay group [white]{settings.GroupId}[/].[/]");
        return 0;
    }
}

// ---------------------------------------------------------------------------
// Leave SyncPlay group
// ---------------------------------------------------------------------------

public sealed class SyncPlayLeaveCommand : ApiCommand<GlobalSettings>
{
    public SyncPlayLeaveCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, GlobalSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        await client.SyncPlay.Leave.PostAsync();

        AnsiConsole.MarkupLine("[green]Left SyncPlay group.[/]");
        return 0;
    }
}

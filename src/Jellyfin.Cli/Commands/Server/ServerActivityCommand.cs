using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Server;

public sealed class ServerActivitySettings : GlobalSettings
{
}

public sealed class ServerActivityCommand : ApiCommand<ServerActivitySettings>
{
    public ServerActivityCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ServerActivitySettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var result = await client.System.ActivityLog.Entries.GetAsync(config =>
        {
            if (settings.Limit.HasValue)
                config.QueryParameters.Limit = settings.Limit.Value;

            if (settings.Start.HasValue)
                config.QueryParameters.StartIndex = settings.Start.Value;
        });

        var entries = result?.Items;

        if (entries is null || entries.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No activity log entries found.[/]");
            return 0;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(result);
            return 0;
        }

        var table = OutputHelper.CreateTable("Date", "Name", "Type", "Severity", "User", "Overview");

        foreach (var entry in entries)
        {
            table.AddRow(
                entry.Date?.ToString("yyyy-MM-dd HH:mm:ss") ?? "",
                Markup.Escape(entry.Name ?? ""),
                Markup.Escape(entry.Type ?? ""),
                entry.Severity?.ToString() ?? "",
                entry.UserId?.ToString() ?? "",
                Markup.Escape(entry.ShortOverview ?? "")
            );
        }

        OutputHelper.WriteTable(table);

        if (result?.TotalRecordCount is not null)
        {
            AnsiConsole.MarkupLine(
                $"[dim]Showing {entries.Count} of {result.TotalRecordCount} total entries.[/]");
        }

        return 0;
    }
}

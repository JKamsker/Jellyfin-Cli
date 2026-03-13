using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Server;

public sealed class ServerLogsSettings : GlobalSettings
{
}

public sealed class ServerLogsCommand : ApiCommand<ServerLogsSettings>
{
    public ServerLogsCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ServerLogsSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var logs = await client.System.Logs.GetAsync();

        if (logs is null || logs.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No log files found.[/]");
            return 0;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(logs);
            return 0;
        }

        var table = OutputHelper.CreateTable("Name", "DateCreated", "DateModified", "Size");

        foreach (var log in logs)
        {
            table.AddRow(
                log.Name ?? "",
                log.DateCreated?.ToString("yyyy-MM-dd HH:mm:ss") ?? "",
                log.DateModified?.ToString("yyyy-MM-dd HH:mm:ss") ?? "",
                FormatFileSize(log.Size)
            );
        }

        OutputHelper.WriteTable(table);
        return 0;
    }

    private static string FormatFileSize(long? bytes)
    {
        if (bytes is null) return "";

        return bytes.Value switch
        {
            >= 1_073_741_824 => $"{bytes.Value / 1_073_741_824.0:F2} GB",
            >= 1_048_576 => $"{bytes.Value / 1_048_576.0:F2} MB",
            >= 1_024 => $"{bytes.Value / 1_024.0:F2} KB",
            _ => $"{bytes.Value} B",
        };
    }
}

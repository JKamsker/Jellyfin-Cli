using System.ComponentModel;
using System.Diagnostics;

using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Server;

public sealed class ServerPingSettings : GlobalSettings
{
    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Server))
            return ValidationResult.Error("--server is required for ping.");

        return ValidationResult.Success();
    }
}

public sealed class ServerPingCommand : AsyncCommand<ServerPingSettings>
{
    private readonly ApiClientFactory _clientFactory;

    public ServerPingCommand(ApiClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ServerPingSettings settings, CancellationToken cancellationToken)
    {
        var client = _clientFactory.CreateClient(settings.Server!);

        var stopwatch = Stopwatch.StartNew();

        string? response;
        try
        {
            response = await client.System.Ping.GetAsync();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            AnsiConsole.MarkupLine($"[red]Ping failed:[/] {ex.Message}");
            return 1;
        }

        stopwatch.Stop();

        if (settings.Json)
        {
            OutputHelper.WriteJson(new
            {
                Response = response,
                ElapsedMs = stopwatch.ElapsedMilliseconds,
            });
            return 0;
        }

        var table = OutputHelper.CreateTable("Field", "Value");
        table.AddRow("Server", settings.Server!);
        table.AddRow("Response", response ?? "(empty)");
        table.AddRow("Time", $"{stopwatch.ElapsedMilliseconds} ms");

        OutputHelper.WriteTable(table);
        return 0;
    }
}

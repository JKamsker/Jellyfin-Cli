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
        // Server can come from credential store at runtime, so don't require it here
        return ValidationResult.Success();
    }
}

public sealed class ServerPingCommand : AsyncCommand<ServerPingSettings>
{
    private readonly ApiClientFactory _clientFactory;
    private readonly CredentialStore _credentialStore;

    public ServerPingCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
    {
        _clientFactory = clientFactory;
        _credentialStore = credentialStore;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ServerPingSettings settings, CancellationToken cancellationToken)
    {
        _credentialStore.ConfigPathOverride = settings.ConfigPath
            ?? Environment.GetEnvironmentVariable("JF_CONFIG");

        var serverInput = settings.Server ?? Environment.GetEnvironmentVariable("JF_SERVER");
        var resolved = _credentialStore.Resolve(serverInput, null);
        var server = resolved?.BaseUrl;

        // Bare hostname fallback
        if (server is null && !string.IsNullOrEmpty(serverInput))
            server = serverInput.Contains("://") ? serverInput : $"https://{serverInput}";

        if (string.IsNullOrEmpty(server))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] --server is required, or run 'jf auth login' first.");
            return 1;
        }

        settings.Server = server;
        var client = _clientFactory.CreateClient(server);

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

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Server;

public sealed class ServerInfoSettings : GlobalSettings
{
}

public sealed class ServerInfoCommand : ApiCommand<ServerInfoSettings>
{
    public ServerInfoCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ServerInfoSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var info = await client.System.Info.GetAsync();

        if (info is null)
        {
            AnsiConsole.MarkupLine("[red]Failed to retrieve server info.[/]");
            return 1;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(info);
            return 0;
        }

        var table = OutputHelper.CreateTable("Field", "Value");
        table.AddRow("ServerName", info.ServerName ?? "");
        table.AddRow("Version", info.Version ?? "");
        table.AddRow("ProductName", info.ProductName ?? "");
        table.AddRow("OperatingSystem", info.OperatingSystem ?? "");
        table.AddRow("OperatingSystemDisplayName", info.OperatingSystemDisplayName ?? "");
        table.AddRow("Architecture", info.SystemArchitecture ?? "");
        table.AddRow("Id", info.Id ?? "");
        table.AddRow("LocalAddress", info.LocalAddress ?? "");
        table.AddRow("HasPendingRestart", info.HasPendingRestart?.ToString() ?? "");
        table.AddRow("IsShuttingDown", info.IsShuttingDown?.ToString() ?? "");
        table.AddRow("StartupWizardCompleted", info.StartupWizardCompleted?.ToString() ?? "");
        table.AddRow("WebSocketPortNumber", info.WebSocketPortNumber?.ToString() ?? "");
        table.AddRow("PackageName", info.PackageName ?? "");

        OutputHelper.WriteTable(table);
        return 0;
    }
}

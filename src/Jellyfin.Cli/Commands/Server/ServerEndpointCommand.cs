using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Server;

public sealed class ServerEndpointCommand : ApiCommand<GlobalSettings>
{
    public ServerEndpointCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, GlobalSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var endpoint = await client.System.Endpoint.GetAsync(cancellationToken: cancellationToken);
        if (endpoint is null)
        {
            AnsiConsole.MarkupLine("[yellow]No endpoint information returned.[/]");
            return 0;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(endpoint);
            return 0;
        }

        var table = OutputHelper.CreateTable("Field", "Value");
        table.AddRow("Is In Network", endpoint.IsInNetwork?.ToString() ?? string.Empty);
        table.AddRow("Is Local", endpoint.IsLocal?.ToString() ?? string.Empty);
        OutputHelper.WriteTable(table);
        return 0;
    }
}

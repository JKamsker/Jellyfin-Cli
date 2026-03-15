using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Server;

public sealed class ServerConfigGetSettings : GlobalSettings
{
    [CommandArgument(0, "[KEY]")]
    [Description("Optional named configuration key")]
    public string? Key { get; set; }
}

public sealed class ServerConfigGetCommand : ApiCommand<ServerConfigGetSettings>
{
    public ServerConfigGetCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ServerConfigGetSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.Key))
        {
            var configuration = await client.System.Configuration.GetAsync(cancellationToken: cancellationToken);
            if (configuration is null)
            {
                AnsiConsole.MarkupLine("[yellow]No configuration returned.[/]");
                return 0;
            }

            OutputHelper.WriteJson(configuration);
            return 0;
        }

        await using var stream = await client.System.Configuration[settings.Key].GetAsync(cancellationToken: cancellationToken);
        if (stream is null)
        {
            AnsiConsole.MarkupLine("[yellow]Configuration section not found.[/]");
            return 0;
        }

        var text = await ResponseOutputHelper.ReadStreamAsStringAsync(stream, cancellationToken);
        ResponseOutputHelper.WriteJsonOrText(text);
        return 0;
    }
}

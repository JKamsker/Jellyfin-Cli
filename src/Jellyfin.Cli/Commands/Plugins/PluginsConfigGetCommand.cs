using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Plugins;

public sealed class PluginsConfigGetSettings : GlobalSettings
{
    [CommandArgument(0, "<PLUGIN_ID>")]
    [Description("Plugin ID (GUID)")]
    public string PluginId { get; set; } = string.Empty;
}

public sealed class PluginsConfigGetCommand : ApiCommand<PluginsConfigGetSettings>
{
    public PluginsConfigGetCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, PluginsConfigGetSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(settings.PluginId, out _))
        {
            Spectre.Console.AnsiConsole.MarkupLine("[red]A valid plugin ID (GUID) is required.[/]");
            return 1;
        }

        using var httpClient = CreateHttpClient();
        using var response = await httpClient.GetAsync(
            $"Plugins/{Uri.EscapeDataString(settings.PluginId)}/Configuration",
            cancellationToken);

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        ResponseOutputHelper.WriteJsonOrText(content);
        return 0;
    }
}

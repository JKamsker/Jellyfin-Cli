using System.ComponentModel;
using System.Text;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Plugins;

public sealed class PluginsConfigSetSettings : GlobalSettings
{
    [CommandArgument(0, "<PLUGIN_ID>")]
    [Description("Plugin ID (GUID)")]
    public string PluginId { get; set; } = string.Empty;

    [CommandOption("--file <FILE>")]
    [Description("Read plugin configuration JSON from a file")]
    public string? FilePath { get; set; }

    [CommandOption("--data <JSON>")]
    [Description("Inline plugin configuration JSON")]
    public string? InlineJson { get; set; }

}

public sealed class PluginsConfigSetCommand : ApiCommand<PluginsConfigSetSettings>
{
    public PluginsConfigSetCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, PluginsConfigSetSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(settings.PluginId, out _))
        {
            AnsiConsole.MarkupLine("[red]A valid plugin ID (GUID) is required.[/]");
            return 1;
        }

        var hasFile = !string.IsNullOrWhiteSpace(settings.FilePath);
        var hasInline = !string.IsNullOrWhiteSpace(settings.InlineJson);
        if (hasFile == hasInline)
        {
            AnsiConsole.MarkupLine("[red]Specify exactly one of --file or --data.[/]");
            return 1;
        }

        var payload = JsonCommandHelper.ReadTextFromFileOrInline(settings.FilePath, settings.InlineJson, "--file/--data");

        using var httpClient = CreateHttpClient();
        using var response = await httpClient.PostAsync(
            $"Plugins/{Uri.EscapeDataString(settings.PluginId)}/Configuration",
            new StringContent(payload, Encoding.UTF8, "application/json"),
            cancellationToken);

        response.EnsureSuccessStatusCode();
        AnsiConsole.MarkupLine($"[green]Updated configuration for plugin [white]{settings.PluginId}[/].[/]");
        return 0;
    }
}

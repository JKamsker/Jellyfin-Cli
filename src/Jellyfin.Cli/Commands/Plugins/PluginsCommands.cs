using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Plugins;

// ---------------------------------------------------------------------------
// List installed plugins
// ---------------------------------------------------------------------------

public sealed class PluginsListCommand : ApiCommand<GlobalSettings>
{
    public PluginsListCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, GlobalSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var plugins = await client.Plugins.GetAsync();

        if (plugins is null || plugins.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No plugins installed.[/]");
            return 0;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(plugins.Select(p => new
            {
                name = p.Name,
                version = p.Version,
                id = p.Id,
                status = p.Status?.ToString(),
            }));
            return 0;
        }

        var table = OutputHelper.CreateTable("Name", "Version", "Id", "Status");
        foreach (var p in plugins)
        {
            table.AddRow(
                p.Name ?? "",
                p.Version ?? "",
                p.Id?.ToString() ?? "",
                p.Status?.ToString() ?? "");
        }

        OutputHelper.WriteTable(table);
        return 0;
    }
}

// ---------------------------------------------------------------------------
// Enable a plugin
// ---------------------------------------------------------------------------

public sealed class PluginVersionSettings : GlobalSettings
{
    [CommandOption("--plugin-id <ID>")]
    [Description("Plugin ID (GUID)")]
    public string PluginId { get; set; } = string.Empty;

    [CommandOption("--version <VERSION>")]
    [Description("Plugin version")]
    public string PluginVersion { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(PluginId))
            return ValidationResult.Error("--plugin-id is required.");

        if (!Guid.TryParse(PluginId, out _))
            return ValidationResult.Error("--plugin-id must be a valid GUID.");

        if (string.IsNullOrWhiteSpace(PluginVersion))
            return ValidationResult.Error("--version is required.");

        return ValidationResult.Success();
    }
}

public sealed class PluginsEnableCommand : ApiCommand<PluginVersionSettings>
{
    public PluginsEnableCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, PluginVersionSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var pluginId = Guid.Parse(settings.PluginId);

        await client.Plugins[pluginId][settings.PluginVersion].Enable.PostAsync();

        AnsiConsole.MarkupLine(
            $"[green]Plugin [white]{settings.PluginId}[/] v{settings.PluginVersion} enabled.[/]");
        return 0;
    }
}

// ---------------------------------------------------------------------------
// Disable a plugin
// ---------------------------------------------------------------------------

public sealed class PluginsDisableCommand : ApiCommand<PluginVersionSettings>
{
    public PluginsDisableCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, PluginVersionSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var pluginId = Guid.Parse(settings.PluginId);

        await client.Plugins[pluginId][settings.PluginVersion].Disable.PostAsync();

        AnsiConsole.MarkupLine(
            $"[green]Plugin [white]{settings.PluginId}[/] v{settings.PluginVersion} disabled.[/]");
        return 0;
    }
}

// ---------------------------------------------------------------------------
// Uninstall a plugin
// ---------------------------------------------------------------------------

public sealed class PluginsUninstallCommand : ApiCommand<PluginVersionSettings>
{
    public PluginsUninstallCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, PluginVersionSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var pluginId = Guid.Parse(settings.PluginId);

        if (!settings.Yes)
        {
            if (!OutputHelper.Confirm(
                    $"Uninstall plugin '{settings.PluginId}' v{settings.PluginVersion}?"))
            {
                AnsiConsole.MarkupLine("[yellow]Cancelled.[/]");
                return 0;
            }
        }

        await client.Plugins[pluginId][settings.PluginVersion].DeleteAsync();

        AnsiConsole.MarkupLine(
            $"[green]Plugin [white]{settings.PluginId}[/] v{settings.PluginVersion} uninstalled.[/]");
        return 0;
    }
}

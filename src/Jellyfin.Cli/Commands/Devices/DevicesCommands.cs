using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Microsoft.Kiota.Abstractions;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Devices;

// ---------------------------------------------------------------------------
// List devices
// ---------------------------------------------------------------------------

public sealed class DevicesListCommand : ApiCommand<GlobalSettings>
{
    public DevicesListCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, GlobalSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var result = await client.Devices.GetAsync();

        var items = result?.Items;
        if (items is null || items.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No devices found.[/]");
            return 0;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(items.Select(d => new
            {
                id = d.Id,
                name = d.Name,
                appName = d.AppName,
                lastUser = d.LastUserName,
                lastActive = d.DateLastActivity,
            }));
            return 0;
        }

        var table = OutputHelper.CreateTable("Id", "Name", "AppName", "LastUser", "LastActive");
        foreach (var d in items)
        {
            table.AddRow(
                d.Id ?? "",
                d.Name ?? "",
                d.AppName ?? "",
                d.LastUserName ?? "",
                d.DateLastActivity?.ToString("g") ?? "");
        }

        OutputHelper.WriteTable(table);
        return 0;
    }
}

// ---------------------------------------------------------------------------
// Get device info
// ---------------------------------------------------------------------------

public sealed class DevicesGetSettings : GlobalSettings
{
    [CommandOption("--id <ID>")]
    [Description("Device ID")]
    public string DeviceId { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(DeviceId))
            return ValidationResult.Error("--id is required.");

        return ValidationResult.Success();
    }
}

public sealed class DevicesGetCommand : ApiCommand<DevicesGetSettings>
{
    public DevicesGetCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, DevicesGetSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var device = await client.Devices.Info.GetAsync(config =>
        {
            config.QueryParameters.Id = settings.DeviceId;
        });

        if (device is null)
        {
            AnsiConsole.MarkupLine("[red]Device not found.[/]");
            return 1;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(new
            {
                id = device.Id,
                name = device.Name,
                customName = device.CustomName,
                appName = device.AppName,
                appVersion = device.AppVersion,
                lastUser = device.LastUserName,
                lastUserId = device.LastUserId,
                lastActive = device.DateLastActivity,
            });
            return 0;
        }

        var table = OutputHelper.CreateTable("Property", "Value");
        table.AddRow("Id", device.Id ?? "");
        table.AddRow("Name", device.Name ?? "");
        table.AddRow("Custom Name", device.CustomName ?? "");
        table.AddRow("App Name", device.AppName ?? "");
        table.AddRow("App Version", device.AppVersion ?? "");
        table.AddRow("Last User", device.LastUserName ?? "");
        table.AddRow("Last User Id", device.LastUserId?.ToString() ?? "");
        table.AddRow("Last Active", device.DateLastActivity?.ToString("g") ?? "");

        OutputHelper.WriteTable(table);
        return 0;
    }
}

// ---------------------------------------------------------------------------
// Delete a device
// ---------------------------------------------------------------------------

public sealed class DevicesDeleteSettings : GlobalSettings
{
    [CommandOption("--id <ID>")]
    [Description("Device ID to delete")]
    public string DeviceId { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(DeviceId))
            return ValidationResult.Error("--id is required.");

        return ValidationResult.Success();
    }
}

public sealed class DevicesDeleteCommand : ApiCommand<DevicesDeleteSettings>
{
    public DevicesDeleteCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, DevicesDeleteSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        if (!settings.Yes)
        {
            if (!OutputHelper.Confirm($"Delete device '{settings.DeviceId}'?"))
            {
                AnsiConsole.MarkupLine("[yellow]Cancelled.[/]");
                return 0;
            }
        }

        await client.Devices.DeleteAsync(config =>
        {
            config.QueryParameters.Id = settings.DeviceId;
        });

        AnsiConsole.MarkupLine("[green]Device deleted.[/]");
        return 0;
    }
}

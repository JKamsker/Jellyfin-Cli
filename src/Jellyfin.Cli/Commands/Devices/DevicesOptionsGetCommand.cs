using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Devices;

public sealed class DevicesOptionsGetSettings : GlobalSettings
{
    [CommandArgument(0, "<DEVICE_ID>")]
    [Description("Device ID")]
    public string DeviceId { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        return string.IsNullOrWhiteSpace(DeviceId)
            ? ValidationResult.Error("A device ID is required.")
            : ValidationResult.Success();
    }
}

public sealed class DevicesOptionsGetCommand : ApiCommand<DevicesOptionsGetSettings>
{
    public DevicesOptionsGetCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, DevicesOptionsGetSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var options = await client.Devices.OptionsPath.GetAsync(config =>
        {
            config.QueryParameters.Id = settings.DeviceId;
        }, cancellationToken);

        if (options is null)
        {
            AnsiConsole.MarkupLine("[yellow]No device options returned.[/]");
            return 0;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(options);
            return 0;
        }

        var table = OutputHelper.CreateTable("Field", "Value");
        table.AddRow("Device ID", options.DeviceId ?? settings.DeviceId);
        table.AddRow("Custom Name", options.CustomName ?? string.Empty);
        table.AddRow("Option ID", options.Id?.ToString() ?? string.Empty);
        OutputHelper.WriteTable(table);
        return 0;
    }
}

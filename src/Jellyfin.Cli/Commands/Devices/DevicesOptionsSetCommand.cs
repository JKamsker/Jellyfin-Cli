using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Api.Generated.Models;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Devices;

public sealed class DevicesOptionsSetSettings : GlobalSettings
{
    [CommandArgument(0, "<DEVICE_ID>")]
    [Description("Device ID")]
    public string DeviceId { get; set; } = string.Empty;

    [CommandOption("--name <NAME>")]
    [Description("Custom display name")]
    public string? CustomName { get; set; }

    [CommandOption("--file <FILE>")]
    [Description("JSON file with DeviceOptionsDto")]
    public string? FilePath { get; set; }

    [CommandOption("--data <JSON>")]
    [Description("Inline JSON with DeviceOptionsDto")]
    public string? InlineJson { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(DeviceId))
            return ValidationResult.Error("A device ID is required.");

        var hasName = !string.IsNullOrWhiteSpace(CustomName);
        var hasFile = !string.IsNullOrWhiteSpace(FilePath);
        var hasInline = !string.IsNullOrWhiteSpace(InlineJson);

        if (hasFile && hasInline)
            return ValidationResult.Error("Specify exactly one of --file or --data.");

        return hasName || hasFile || hasInline
            ? ValidationResult.Success()
            : ValidationResult.Error("Specify --name, --file, or --data.");
    }
}

public sealed class DevicesOptionsSetCommand : ApiCommand<DevicesOptionsSetSettings>
{
    public DevicesOptionsSetCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, DevicesOptionsSetSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var body = !string.IsNullOrWhiteSpace(settings.FilePath) || !string.IsNullOrWhiteSpace(settings.InlineJson)
            ? JsonCommandHelper.DeserializeFromFileOrInline<DeviceOptionsDto>(settings.FilePath, settings.InlineJson, "--file/--data")
            : new DeviceOptionsDto();

        if (!string.IsNullOrWhiteSpace(settings.CustomName))
            body.CustomName = settings.CustomName;

        body.DeviceId ??= settings.DeviceId;

        await client.Devices.OptionsPath.PostAsync(body, config =>
        {
            config.QueryParameters.Id = settings.DeviceId;
        }, cancellationToken);

        AnsiConsole.MarkupLine($"[green]Updated device options for [white]{Markup.Escape(settings.DeviceId)}[/].[/]");
        return 0;
    }
}

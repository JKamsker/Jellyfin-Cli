using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Api.Generated.Models;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Server;

public sealed class ServerConfigBrandingSettings : GlobalSettings
{
    [CommandOption("--file <FILE>")]
    [Description("Read branding JSON from a file")]
    public string? FilePath { get; set; }

    [CommandOption("--data <JSON>")]
    [Description("Inline branding JSON")]
    public string? InlineJson { get; set; }

    [CommandOption("--login-disclaimer <TEXT>")]
    [Description("Override the login disclaimer")]
    public string? LoginDisclaimer { get; set; }

    [CommandOption("--custom-css <TEXT>")]
    [Description("Override the custom CSS")]
    public string? CustomCss { get; set; }

    [CommandOption("--splashscreen-enabled")]
    [Description("Enable the splash screen")]
    public bool EnableSplashscreen { get; set; }

    [CommandOption("--splashscreen-disabled")]
    [Description("Disable the splash screen")]
    public bool DisableSplashscreen { get; set; }

    public bool IsUpdateRequested =>
        !string.IsNullOrWhiteSpace(FilePath) ||
        !string.IsNullOrWhiteSpace(InlineJson) ||
        LoginDisclaimer is not null ||
        CustomCss is not null ||
        EnableSplashscreen ||
        DisableSplashscreen;

    public override ValidationResult Validate()
    {
        if (EnableSplashscreen && DisableSplashscreen)
            return ValidationResult.Error("Use either --splashscreen-enabled or --splashscreen-disabled, not both.");

        if ((!string.IsNullOrWhiteSpace(FilePath) || !string.IsNullOrWhiteSpace(InlineJson)) &&
            (LoginDisclaimer is not null || CustomCss is not null || EnableSplashscreen || DisableSplashscreen))
        {
            return ValidationResult.Error("Use either --file/--data or the individual branding flags, not both.");
        }

        return ValidationResult.Success();
    }
}

public sealed class ServerConfigBrandingCommand : ApiCommand<ServerConfigBrandingSettings>
{
    public ServerConfigBrandingCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ServerConfigBrandingSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        if (!settings.IsUpdateRequested)
        {
            var branding = await client.Branding.Configuration.GetAsync(cancellationToken: cancellationToken);
            if (branding is null)
            {
                AnsiConsole.MarkupLine("[yellow]No branding configuration returned.[/]");
                return 0;
            }

            OutputHelper.WriteJson(branding);
            return 0;
        }

        BrandingOptionsDto brandingOptions;
        if (!string.IsNullOrWhiteSpace(settings.FilePath) || !string.IsNullOrWhiteSpace(settings.InlineJson))
        {
            brandingOptions = JsonCommandHelper.DeserializeFromFileOrInline<BrandingOptionsDto>(
                settings.FilePath,
                settings.InlineJson,
                "--file/--data");
        }
        else
        {
            brandingOptions = await client.Branding.Configuration.GetAsync(cancellationToken: cancellationToken)
                ?? new BrandingOptionsDto();

            if (settings.LoginDisclaimer is not null)
                brandingOptions.LoginDisclaimer = settings.LoginDisclaimer;

            if (settings.CustomCss is not null)
                brandingOptions.CustomCss = settings.CustomCss;

            if (settings.EnableSplashscreen)
                brandingOptions.SplashscreenEnabled = true;

            if (settings.DisableSplashscreen)
                brandingOptions.SplashscreenEnabled = false;
        }

        await client.System.Configuration.Branding.PostAsync(brandingOptions, cancellationToken: cancellationToken);
        AnsiConsole.MarkupLine("[green]Updated server branding configuration.[/]");
        return 0;
    }
}

using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Packages;

public sealed class PackagesInstallSettings : GlobalSettings
{
    [CommandArgument(0, "<NAME>")]
    [Description("Package name")]
    public string Name { get; set; } = string.Empty;

    [CommandOption("--assembly-guid <GUID>")]
    [Description("Assembly GUID to disambiguate package lookups")]
    public string? AssemblyGuid { get; set; }

    [CommandOption("--version <VERSION>")]
    [Description("Install a specific version instead of the latest")]
    public string? Version { get; set; }

    [CommandOption("--repository-url <URL>")]
    [Description("Install from a specific repository URL")]
    public string? RepositoryUrl { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return ValidationResult.Error("A package name is required.");

        if (!string.IsNullOrWhiteSpace(AssemblyGuid) && !Guid.TryParse(AssemblyGuid, out _))
            return ValidationResult.Error("--assembly-guid must be a valid GUID.");

        return ValidationResult.Success();
    }
}

public sealed class PackagesInstallCommand : ApiCommand<PackagesInstallSettings>
{
    public PackagesInstallCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, PackagesInstallSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        await client.Packages.Installed[settings.Name].PostAsync(config =>
        {
            if (Guid.TryParse(settings.AssemblyGuid, out var assemblyGuid))
                config.QueryParameters.AssemblyGuid = assemblyGuid;

            config.QueryParameters.Version = settings.Version;
            config.QueryParameters.RepositoryUrl = settings.RepositoryUrl;
        }, cancellationToken);

        var versionText = string.IsNullOrWhiteSpace(settings.Version)
            ? "latest version"
            : $"version {settings.Version}";

        AnsiConsole.MarkupLine(
            $"[green]Started installing package [white]{Markup.Escape(settings.Name)}[/] ({Markup.Escape(versionText)}).[/]");
        return 0;
    }
}

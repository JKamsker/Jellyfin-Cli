using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Packages;

public sealed class PackagesGetSettings : GlobalSettings
{
    [CommandArgument(0, "<NAME>")]
    [Description("Package name")]
    public string Name { get; set; } = string.Empty;

    [CommandOption("--assembly-guid <GUID>")]
    [Description("Assembly GUID to disambiguate package lookups")]
    public string? AssemblyGuid { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return ValidationResult.Error("A package name is required.");

        if (!string.IsNullOrWhiteSpace(AssemblyGuid) && !Guid.TryParse(AssemblyGuid, out _))
            return ValidationResult.Error("--assembly-guid must be a valid GUID.");

        return ValidationResult.Success();
    }
}

public sealed class PackagesGetCommand : ApiCommand<PackagesGetSettings>
{
    public PackagesGetCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, PackagesGetSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var package = await client.Packages[settings.Name].GetAsync(config =>
        {
            if (Guid.TryParse(settings.AssemblyGuid, out var assemblyGuid))
                config.QueryParameters.AssemblyGuid = assemblyGuid;
        }, cancellationToken);

        if (package is null)
        {
            AnsiConsole.MarkupLine("[yellow]Package not found.[/]");
            return 0;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(package);
            return 0;
        }

        var table = OutputHelper.CreateTable("Field", "Value");
        table.AddRow("Name", Markup.Escape(package.Name ?? string.Empty));
        table.AddRow("Owner", Markup.Escape(package.Owner ?? string.Empty));
        table.AddRow("Category", Markup.Escape(package.Category ?? string.Empty));
        table.AddRow("Assembly Guid", package.Guid?.ToString() ?? string.Empty);
        table.AddRow("Image Url", Markup.Escape(package.ImageUrl ?? string.Empty));
        table.AddRow("Overview", Markup.Escape(package.Overview ?? string.Empty));
        table.AddRow("Description", Markup.Escape(package.Description ?? string.Empty));
        table.AddRow("Versions", (package.Versions?.Count ?? 0).ToString());
        OutputHelper.WriteTable(table);

        if (package.Versions is { Count: > 0 })
        {
            var versionsTable = OutputHelper.CreateTable("Version", "Repository", "Repository Url", "Target ABI", "Timestamp");
            foreach (var version in package.Versions)
            {
                versionsTable.AddRow(
                    Markup.Escape(version.Version ?? string.Empty),
                    Markup.Escape(version.RepositoryName ?? string.Empty),
                    Markup.Escape(version.RepositoryUrl ?? string.Empty),
                    Markup.Escape(version.TargetAbi ?? string.Empty),
                    Markup.Escape(version.Timestamp ?? string.Empty));
            }

            OutputHelper.WriteTable(versionsTable);
        }

        return 0;
    }
}

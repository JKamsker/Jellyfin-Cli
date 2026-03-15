using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Packages;

public sealed class PackagesListCommand : ApiCommand<GlobalSettings>
{
    public PackagesListCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, GlobalSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var packages = await client.Packages.GetAsync(cancellationToken: cancellationToken);

        if (packages is null || packages.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No packages available from configured repositories.[/]");
            return 0;
        }

        var ordered = packages
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (settings.Json)
        {
            OutputHelper.WriteJson(ordered);
            return 0;
        }

        var table = OutputHelper.CreateTable("Name", "Owner", "Category", "Latest Version", "Versions");
        foreach (var package in ordered)
        {
            var versions = package.Versions ?? [];
            table.AddRow(
                Markup.Escape(package.Name ?? string.Empty),
                Markup.Escape(package.Owner ?? string.Empty),
                Markup.Escape(package.Category ?? string.Empty),
                Markup.Escape(versions.FirstOrDefault()?.Version ?? string.Empty),
                versions.Count.ToString());
        }

        OutputHelper.WriteTable(table);
        return 0;
    }
}

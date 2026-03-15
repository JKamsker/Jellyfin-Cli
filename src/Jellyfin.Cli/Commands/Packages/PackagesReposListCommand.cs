using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Packages;

public sealed class PackagesReposListCommand : ApiCommand<GlobalSettings>
{
    public PackagesReposListCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, GlobalSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var repositories = await client.Repositories.GetAsync(cancellationToken: cancellationToken);

        if (repositories is null || repositories.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No package repositories configured.[/]");
            return 0;
        }

        var ordered = repositories
            .OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (settings.Json)
        {
            OutputHelper.WriteJson(ordered);
            return 0;
        }

        var table = OutputHelper.CreateTable("Name", "Url", "Enabled");
        foreach (var repository in ordered)
        {
            table.AddRow(
                Markup.Escape(repository.Name ?? string.Empty),
                Markup.Escape(repository.Url ?? string.Empty),
                repository.Enabled?.ToString() ?? string.Empty);
        }

        OutputHelper.WriteTable(table);
        return 0;
    }
}

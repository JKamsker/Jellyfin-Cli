using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Api.Generated.Models;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Packages;

public sealed class PackagesReposSetSettings : GlobalSettings
{
    [CommandOption("--file <FILE>")]
    [Description("Read the full repository list from a JSON file")]
    public string? FilePath { get; set; }

    [CommandOption("--data <JSON>")]
    [Description("Inline JSON array for the full repository list")]
    public string? InlineJson { get; set; }

    public override ValidationResult Validate()
    {
        var hasFile = !string.IsNullOrWhiteSpace(FilePath);
        var hasInline = !string.IsNullOrWhiteSpace(InlineJson);

        return hasFile == hasInline
            ? ValidationResult.Error("Specify exactly one of --file or --data.")
            : ValidationResult.Success();
    }
}

public sealed class PackagesReposSetCommand : ApiCommand<PackagesReposSetSettings>
{
    public PackagesReposSetCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, PackagesReposSetSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var repositories = JsonCommandHelper.DeserializeFromFileOrInline<List<RepositoryInfo>>(
            settings.FilePath,
            settings.InlineJson,
            "--file/--data");

        await client.Repositories.PostAsync(repositories, cancellationToken: cancellationToken);

        AnsiConsole.MarkupLine($"[green]Updated {repositories.Count} package repositories.[/]");
        return 0;
    }
}

using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Server;

public sealed class ServerEnvironmentLsSettings : GlobalSettings
{
    [CommandArgument(0, "<PATH>")]
    [Description("Directory path on the server")]
    public string Path { get; set; } = string.Empty;

    [CommandOption("--files")]
    [Description("Include files")]
    public bool Files { get; set; }

    [CommandOption("--directories")]
    [Description("Include directories")]
    public bool Directories { get; set; }

    public override ValidationResult Validate()
    {
        return string.IsNullOrWhiteSpace(Path)
            ? ValidationResult.Error("A directory path is required.")
            : ValidationResult.Success();
    }
}

public sealed class ServerEnvironmentLsCommand : ApiCommand<ServerEnvironmentLsSettings>
{
    public ServerEnvironmentLsCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ServerEnvironmentLsSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var includeFiles = settings.Files || !settings.Directories;
        var includeDirectories = settings.Directories || !settings.Files;

        var entries = await client.Environment.DirectoryContents.GetAsync(config =>
        {
            config.QueryParameters.Path = settings.Path;
            config.QueryParameters.IncludeFiles = includeFiles;
            config.QueryParameters.IncludeDirectories = includeDirectories;
        }, cancellationToken) ?? [];

        if (entries.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No directory entries found.[/]");
            return 0;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(entries);
            return 0;
        }

        var table = OutputHelper.CreateTable("Type", "Name", "Path");
        foreach (var entry in entries.OrderBy(e => e.Type?.ToString()).ThenBy(e => e.Name))
        {
            table.AddRow(
                entry.Type?.ToString() ?? string.Empty,
                Markup.Escape(entry.Name ?? string.Empty),
                Markup.Escape(entry.Path ?? string.Empty));
        }

        OutputHelper.WriteTable(table);
        return 0;
    }
}

using System.ComponentModel;
using System.Text;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Server;

public sealed class ServerLogGetSettings : GlobalSettings
{
    [CommandArgument(0, "<NAME>")]
    [Description("Log file name")]
    public string Name { get; set; } = string.Empty;

    [CommandOption("--out <FILE>")]
    [Description("Write log contents to a file instead of stdout")]
    public string? OutputPath { get; set; }

    public override ValidationResult Validate()
    {
        return string.IsNullOrWhiteSpace(Name)
            ? ValidationResult.Error("A log file name is required.")
            : ValidationResult.Success();
    }
}

public sealed class ServerLogGetCommand : ApiCommand<ServerLogGetSettings>
{
    public ServerLogGetCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ServerLogGetSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        await using var stream = await client.System.Logs.Log.GetAsync(config =>
        {
            config.QueryParameters.Name = settings.Name;
        }, cancellationToken);

        if (stream is null)
        {
            AnsiConsole.MarkupLine("[yellow]Log file not found.[/]");
            return 0;
        }

        if (!string.IsNullOrWhiteSpace(settings.OutputPath))
        {
            await using var fileStream = File.Create(settings.OutputPath);
            await stream.CopyToAsync(fileStream, cancellationToken);
            AnsiConsole.MarkupLine($"[green]Saved log to [white]{Markup.Escape(settings.OutputPath)}[/].[/]");
            return 0;
        }

        using var reader = new StreamReader(stream, Encoding.UTF8);
        AnsiConsole.WriteLine(await reader.ReadToEndAsync(cancellationToken));
        return 0;
    }
}

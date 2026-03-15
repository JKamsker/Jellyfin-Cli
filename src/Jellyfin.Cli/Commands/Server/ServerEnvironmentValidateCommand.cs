using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Api.Generated.Models;
using Jellyfin.Cli.Common;

using Microsoft.Kiota.Abstractions;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Server;

public sealed class ServerEnvironmentValidateSettings : GlobalSettings
{
    [CommandArgument(0, "<PATH>")]
    [Description("Path on the server")]
    public string Path { get; set; } = string.Empty;

    [CommandOption("--file")]
    [Description("Validate the path as a file instead of a directory")]
    public bool IsFile { get; set; }

    [CommandOption("--writable")]
    [Description("Require the path to be writable")]
    public bool ValidateWritable { get; set; }

    public override ValidationResult Validate()
    {
        return string.IsNullOrWhiteSpace(Path)
            ? ValidationResult.Error("A path is required.")
            : ValidationResult.Success();
    }
}

public sealed class ServerEnvironmentValidateCommand : ApiCommand<ServerEnvironmentValidateSettings>
{
    public ServerEnvironmentValidateCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ServerEnvironmentValidateSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        try
        {
            await client.Environment.ValidatePath.PostAsync(new ValidatePathDto
            {
                Path = settings.Path,
                IsFile = settings.IsFile ? true : null,
                ValidateWritable = settings.ValidateWritable ? true : null,
            }, cancellationToken: cancellationToken);
        }
        catch (ApiException ex) when (ex.ResponseStatusCode is 400 or 404)
        {
            if (settings.Json)
            {
                OutputHelper.WriteJson(new
                {
                    path = settings.Path,
                    isFile = settings.IsFile,
                    validateWritable = settings.ValidateWritable,
                    valid = false,
                    error = ex.Message,
                });
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Path is invalid:[/] {Markup.Escape(settings.Path)}");
                AnsiConsole.MarkupLine($"[dim]{Markup.Escape(ex.Message)}[/]");
            }

            return 1;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(new
            {
                path = settings.Path,
                isFile = settings.IsFile,
                validateWritable = settings.ValidateWritable,
                valid = true,
            });
            return 0;
        }

        AnsiConsole.MarkupLine($"[green]Path is valid:[/] {Markup.Escape(settings.Path)}");
        return 0;
    }
}

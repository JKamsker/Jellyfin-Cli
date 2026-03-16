using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Auth.Quick;

public sealed class QuickApproveSettings : GlobalSettings
{
    [CommandOption("--code <CODE>")]
    [Description("The Quick Connect code to approve")]
    public string Code { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Code))
            return ValidationResult.Error("--code is required.");

        return ValidationResult.Success();
    }
}

public sealed class QuickApproveCommand : ApiCommand<QuickApproveSettings>
{
    public QuickApproveCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, QuickApproveSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        try
        {
            var result = await client.QuickConnect.Authorize.PostAsync(cfg =>
            {
                cfg.QueryParameters.Code = settings.Code;
            });

            if (result == true)
            {
                AnsiConsole.MarkupLine($"[green]Quick Connect code '[white]{settings.Code}[/]' approved.[/]");
                return 0;
            }

            AnsiConsole.MarkupLine("[red]Authorization was not confirmed by the server.[/]");
            return 1;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to approve Quick Connect code:[/] {ex.Message}");
            return 1;
        }
    }
}

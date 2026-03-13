using System.ComponentModel;

using Jellyfin.Cli.Api.Generated.Models;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Auth;

public sealed class LoginSettings : GlobalSettings
{
    [CommandOption("-u|--username <USERNAME>")]
    [Description("Jellyfin username")]
    public string Username { get; set; } = string.Empty;

    [CommandOption("-p|--password <PASSWORD>")]
    [Description("Password (prompted if omitted)")]
    public string? Password { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Server))
            return ValidationResult.Error("--server is required for login.");

        if (string.IsNullOrWhiteSpace(Username))
            return ValidationResult.Error("--username is required.");

        return ValidationResult.Success();
    }
}

public sealed class LoginCommand : AsyncCommand<LoginSettings>
{
    private readonly ApiClientFactory _clientFactory;
    private readonly CredentialStore _credentialStore;

    public LoginCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
    {
        _clientFactory = clientFactory;
        _credentialStore = credentialStore;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, LoginSettings settings, CancellationToken cancellationToken)
    {
        var password = settings.Password;
        if (string.IsNullOrEmpty(password))
        {
            password = AnsiConsole.Prompt(
                new TextPrompt<string>("Password:")
                    .Secret());
        }

        var client = _clientFactory.CreateClient(settings.Server!);

        var body = new AuthenticateUserByName
        {
            Username = settings.Username,
            Pw = password,
        };

        AuthenticationResult? result;
        try
        {
            result = await client.Users.AuthenticateByName.PostAsync(body);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Login failed:[/] {ex.Message}");
            return 1;
        }

        if (result?.AccessToken is null || result.User is null)
        {
            AnsiConsole.MarkupLine("[red]Login failed:[/] No access token returned.");
            return 1;
        }

        _credentialStore.Save(new StoredCredentials
        {
            Server = settings.Server!,
            Token = result.AccessToken,
            UserId = result.User.Id?.ToString() ?? string.Empty,
            UserName = result.User.Name ?? string.Empty,
        });

        AnsiConsole.MarkupLine("[green]Logged in successfully.[/]");

        var table = OutputHelper.CreateTable("Field", "Value");
        table.AddRow("Server", settings.Server!);
        table.AddRow("User", result.User.Name ?? "(unknown)");
        table.AddRow("User ID", result.User.Id?.ToString() ?? "(unknown)");
        OutputHelper.WriteTable(table);

        return 0;
    }
}

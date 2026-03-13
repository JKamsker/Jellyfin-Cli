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
    public string? Username { get; set; }

    [CommandOption("-p|--password <PASSWORD>")]
    [Description("Password (prompted if omitted)")]
    public string? Password { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Server))
            return ValidationResult.Error("--server is required for login.");

        // Either --api-key or --username must be provided
        if (string.IsNullOrWhiteSpace(ApiKey) && string.IsNullOrWhiteSpace(Username))
            return ValidationResult.Error("--username or --api-key is required.");

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
        // API key login: just store and verify
        if (!string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            return await LoginWithApiKey(settings);
        }

        return await LoginWithPassword(settings);
    }

    private async Task<int> LoginWithApiKey(LoginSettings settings)
    {
        var client = _clientFactory.CreateClient(settings.Server!, apiKey: settings.ApiKey);

        try
        {
            var info = await client.System.Info.GetAsync();
            _credentialStore.Save(new StoredCredentials
            {
                Server = settings.Server!,
                ApiKey = settings.ApiKey!,
            });

            AnsiConsole.MarkupLine("[green]API key saved successfully.[/]");
            var table = OutputHelper.CreateTable("Field", "Value");
            table.AddRow("Server", settings.Server!);
            table.AddRow("Server Name", info?.ServerName ?? "(unknown)");
            table.AddRow("Auth", "API Key");
            OutputHelper.WriteTable(table);
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]API key verification failed:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }

    private async Task<int> LoginWithPassword(LoginSettings settings)
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
            AnsiConsole.MarkupLine($"[red]Login failed:[/] {Markup.Escape(ex.Message)}");
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

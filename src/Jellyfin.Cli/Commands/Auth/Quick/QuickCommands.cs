using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Api.Generated.Models;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Auth.Quick;

// ---------------------------------------------------------------------------
// Quick Connect Login -- initiate, poll, then authenticate
// ---------------------------------------------------------------------------

public sealed class QuickLoginCommand : ApiCommand<GlobalSettings>
{
    private readonly CredentialStore _credentialStore;

    public QuickLoginCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
        _credentialStore = credentialStore;
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, GlobalSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        // Check if Quick Connect is enabled
        var enabled = await client.QuickConnect.Enabled.GetAsync();
        if (enabled != true)
        {
            AnsiConsole.MarkupLine("[red]Quick Connect is not enabled on this server.[/]");
            return 1;
        }

        // Initiate a new Quick Connect request
        var initResult = await client.QuickConnect.Initiate.PostAsync();
        if (initResult?.Code is null || initResult.Secret is null)
        {
            AnsiConsole.MarkupLine("[red]Failed to initiate Quick Connect.[/]");
            return 1;
        }

        AnsiConsole.MarkupLine($"[bold]Quick Connect code:[/] [cyan]{initResult.Code}[/]");
        AnsiConsole.MarkupLine("Enter the code above in another Jellyfin client or the web UI to authorize this login.");

        // Poll for authorization
        var authorized = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Waiting for authorization...", async ctx =>
            {
                for (var i = 0; i < 120; i++) // up to ~2 minutes
                {
                    await Task.Delay(1000);

                    var status = await client.QuickConnect.Connect.GetAsync(cfg =>
                    {
                        cfg.QueryParameters.Secret = initResult.Secret;
                    });

                    if (status?.Authenticated == true)
                        return true;
                }

                return false;
            });

        if (!authorized)
        {
            AnsiConsole.MarkupLine("[red]Quick Connect authorization timed out.[/]");
            return 1;
        }

        // Authenticate with the secret
        var authResult = await client.Users.AuthenticateWithQuickConnect.PostAsync(
            new QuickConnectDto { Secret = initResult.Secret });

        if (authResult?.AccessToken is null || authResult.User is null)
        {
            AnsiConsole.MarkupLine("[red]Quick Connect authentication failed.[/]");
            return 1;
        }

        var stored = _credentialStore.Load();
        var serverUrl = settings.Server ?? stored?.Server ?? string.Empty;

        // Determine profile name
        var profileName = settings.Profile;
        if (string.IsNullOrEmpty(profileName))
        {
            try
            {
                var uri = new Uri(serverUrl);
                profileName = uri.Host;
            }
            catch
            {
                profileName = "default";
            }
        }

        _credentialStore.SaveProfile(profileName, new StoredCredentials
        {
            Server = serverUrl,
            Token = authResult.AccessToken,
            UserId = authResult.User.Id?.ToString() ?? string.Empty,
            UserName = authResult.User.Name ?? string.Empty,
        });

        AnsiConsole.MarkupLine($"[green]Logged in via Quick Connect. Profile '{Markup.Escape(profileName)}' saved.[/]");

        var table = OutputHelper.CreateTable("Field", "Value");
        table.AddRow("Profile", profileName);
        table.AddRow("Server", serverUrl);
        table.AddRow("User", authResult.User.Name ?? "(unknown)");
        table.AddRow("User ID", authResult.User.Id?.ToString() ?? "(unknown)");
        OutputHelper.WriteTable(table);

        return 0;
    }
}

// ---------------------------------------------------------------------------
// Quick Connect Approve -- authorize a pending code (requires existing auth)
// ---------------------------------------------------------------------------

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

// ---------------------------------------------------------------------------
// Quick Connect Status -- check if Quick Connect is enabled
// ---------------------------------------------------------------------------

public sealed class QuickStatusCommand : ApiCommand<GlobalSettings>
{
    public QuickStatusCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, GlobalSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var enabled = await client.QuickConnect.Enabled.GetAsync();

        if (settings.Json)
        {
            OutputHelper.WriteJson(new { enabled });
            return 0;
        }

        if (enabled == true)
        {
            AnsiConsole.MarkupLine("[green]Quick Connect is enabled.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]Quick Connect is disabled.[/]");
        }

        return 0;
    }
}

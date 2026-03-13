using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Api.Generated.Auth.Keys;
using Jellyfin.Cli.Common;

using Microsoft.Kiota.Abstractions;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Auth;

// ---------------------------------------------------------------------------
// List API keys
// ---------------------------------------------------------------------------

public sealed class KeysListCommand : ApiCommand<GlobalSettings>
{
    public KeysListCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, GlobalSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var result = await client.Auth.Keys.GetAsync();

        var items = result?.Items;
        if (items is null || items.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No API keys found.[/]");
            return 0;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(items.Select(k => new
            {
                accessToken = k.AccessToken,
                appName = k.AppName,
                dateCreated = k.DateCreated,
            }));
            return 0;
        }

        var table = OutputHelper.CreateTable("App Name", "Access Token", "Date Created");
        foreach (var key in items)
        {
            table.AddRow(
                key.AppName ?? "(unknown)",
                key.AccessToken ?? "(unknown)",
                key.DateCreated?.ToString("u") ?? "(unknown)");
        }

        OutputHelper.WriteTable(table);
        return 0;
    }
}

// ---------------------------------------------------------------------------
// Create an API key
// ---------------------------------------------------------------------------

public sealed class KeysCreateSettings : GlobalSettings
{
    [CommandOption("--app <APP>")]
    [Description("Name of the application requesting the key")]
    public string App { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(App))
            return ValidationResult.Error("--app is required.");

        return ValidationResult.Success();
    }
}

public sealed class KeysCreateCommand : ApiCommand<KeysCreateSettings>
{
    public KeysCreateCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, KeysCreateSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        await client.Auth.Keys.PostAsync(cfg =>
        {
            cfg.QueryParameters.App = settings.App;
        });

        AnsiConsole.MarkupLine($"[green]API key created for app '[white]{settings.App}[/]'.[/]");
        return 0;
    }
}

// ---------------------------------------------------------------------------
// Delete an API key
// ---------------------------------------------------------------------------

public sealed class KeysDeleteSettings : GlobalSettings
{
    [CommandOption("--key <KEY>")]
    [Description("The access token (API key) to revoke")]
    public string Key { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Key))
            return ValidationResult.Error("--key is required.");

        return ValidationResult.Success();
    }
}

public sealed class KeysDeleteCommand : ApiCommand<KeysDeleteSettings>
{
    public KeysDeleteCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, KeysDeleteSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        if (!settings.Yes)
        {
            if (!OutputHelper.Confirm($"Revoke API key '{settings.Key}'?"))
            {
                AnsiConsole.MarkupLine("[yellow]Cancelled.[/]");
                return 0;
            }
        }

        await client.Auth.Keys[settings.Key].DeleteAsync();

        AnsiConsole.MarkupLine("[green]API key revoked.[/]");
        return 0;
    }
}

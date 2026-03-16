using System.ComponentModel;

using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Auth;

// ---------------------------------------------------------------------------
// auth profiles list
// ---------------------------------------------------------------------------

public sealed class ProfilesListCommand : AsyncCommand<GlobalSettings>
{
    private readonly CredentialStore _credentialStore;

    public ProfilesListCommand(CredentialStore credentialStore)
    {
        _credentialStore = credentialStore;
    }

    public override Task<int> ExecuteAsync(CommandContext context, GlobalSettings settings, CancellationToken cancellationToken)
    {
        var profiles = _credentialStore.GetProfiles();
        var active = _credentialStore.GetActiveProfileName();

        if (profiles.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No profiles configured. Run 'jf auth login' to create one.[/]");
            return Task.FromResult(0);
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(profiles.Select(p => new
            {
                name = p.Key,
                server = p.Value.Server,
                user = p.Value.UserName,
                auth = DescribeAuth(p.Value),
                active = p.Key == active,
            }));
            return Task.FromResult(0);
        }

        var table = OutputHelper.CreateTable("", "Name", "Server", "User", "Auth");
        foreach (var (name, profile) in profiles)
        {
            var marker = name == active ? "[green]*[/]" : "";
            table.AddRow(
                marker,
                Markup.Escape(name),
                Markup.Escape(profile.Server),
                Markup.Escape(profile.UserName),
                DescribeAuth(profile));
        }
        OutputHelper.WriteTable(table);

        return Task.FromResult(0);
    }

    private static string DescribeAuth(StoredCredentials creds)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(creds.Token))
            parts.Add("token");
        if (!string.IsNullOrEmpty(creds.ApiKey))
            parts.Add("api-key");
        if (!string.IsNullOrEmpty(creds.Password))
            parts.Add("password");
        return parts.Count > 0 ? string.Join(", ", parts) : "none";
    }
}

// ---------------------------------------------------------------------------
// auth profiles use <name>
// ---------------------------------------------------------------------------

public sealed class ProfilesUseSettings : GlobalSettings
{
    [CommandArgument(0, "<NAME>")]
    [Description("Profile name to activate")]
    public string Name { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return ValidationResult.Error("Profile name is required.");
        return ValidationResult.Success();
    }
}

public sealed class ProfilesUseCommand : AsyncCommand<ProfilesUseSettings>
{
    private readonly CredentialStore _credentialStore;

    public ProfilesUseCommand(CredentialStore credentialStore)
    {
        _credentialStore = credentialStore;
    }

    public override Task<int> ExecuteAsync(CommandContext context, ProfilesUseSettings settings, CancellationToken cancellationToken)
    {
        var profiles = _credentialStore.GetProfiles();
        if (!profiles.ContainsKey(settings.Name))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Profile '{Markup.Escape(settings.Name)}' does not exist.");
            if (profiles.Count > 0)
                AnsiConsole.MarkupLine($"[dim]Available:[/] {string.Join(", ", profiles.Keys)}");
            return Task.FromResult(1);
        }

        _credentialStore.SetActiveProfile(settings.Name);
        var profile = profiles[settings.Name];
        AnsiConsole.MarkupLine($"[green]Active profile set to '{Markup.Escape(settings.Name)}'.[/]");
        AnsiConsole.MarkupLine($"[dim]Server:[/] {Markup.Escape(profile.Server)}");
        if (!string.IsNullOrEmpty(profile.UserName))
            AnsiConsole.MarkupLine($"[dim]User:[/] {Markup.Escape(profile.UserName)}");

        return Task.FromResult(0);
    }
}

// ---------------------------------------------------------------------------
// auth profiles show <name>
// ---------------------------------------------------------------------------

public sealed class ProfilesShowSettings : GlobalSettings
{
    [CommandArgument(0, "<NAME>")]
    [Description("Profile name to inspect")]
    public string Name { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return ValidationResult.Error("Profile name is required.");
        return ValidationResult.Success();
    }
}

public sealed class ProfilesShowCommand : AsyncCommand<ProfilesShowSettings>
{
    private readonly CredentialStore _credentialStore;

    public ProfilesShowCommand(CredentialStore credentialStore)
    {
        _credentialStore = credentialStore;
    }

    public override Task<int> ExecuteAsync(CommandContext context, ProfilesShowSettings settings, CancellationToken cancellationToken)
    {
        var profile = _credentialStore.LoadProfile(settings.Name);
        if (profile is null)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Profile '{Markup.Escape(settings.Name)}' does not exist.");
            return Task.FromResult(1);
        }

        var active = _credentialStore.GetActiveProfileName();
        var isActive = settings.Name == active;

        if (settings.Json)
        {
            OutputHelper.WriteJson(new
            {
                name = settings.Name,
                server = profile.Server,
                user = profile.UserName,
                userId = profile.UserId,
                hasToken = !string.IsNullOrEmpty(profile.Token),
                hasApiKey = !string.IsNullOrEmpty(profile.ApiKey),
                hasPassword = !string.IsNullOrEmpty(profile.Password),
                active = isActive,
            });
            return Task.FromResult(0);
        }

        var table = OutputHelper.CreateTable("Field", "Value");
        table.AddRow("Name", settings.Name);
        table.AddRow("Active", isActive ? "Yes" : "No");
        table.AddRow("Server", profile.Server);
        table.AddRow("User", profile.UserName);
        table.AddRow("User ID", profile.UserId);
        table.AddRow("Token", RedactSecret(profile.Token));
        table.AddRow("API Key", RedactSecret(profile.ApiKey));
        table.AddRow("Username", profile.Username);
        table.AddRow("Password", string.IsNullOrEmpty(profile.Password) ? "" : "********");
        OutputHelper.WriteTable(table);

        return Task.FromResult(0);
    }

    private static string RedactSecret(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        if (value.Length <= 8)
            return "****";
        return value[..4] + "..." + value[^4..];
    }
}

// ---------------------------------------------------------------------------
// auth profiles delete <name>
// ---------------------------------------------------------------------------

public sealed class ProfilesDeleteSettings : GlobalSettings
{
    [CommandArgument(0, "<NAME>")]
    [Description("Profile name to delete")]
    public string Name { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return ValidationResult.Error("Profile name is required.");
        return ValidationResult.Success();
    }
}

public sealed class ProfilesDeleteCommand : AsyncCommand<ProfilesDeleteSettings>
{
    private readonly CredentialStore _credentialStore;

    public ProfilesDeleteCommand(CredentialStore credentialStore)
    {
        _credentialStore = credentialStore;
    }

    public override Task<int> ExecuteAsync(CommandContext context, ProfilesDeleteSettings settings, CancellationToken cancellationToken)
    {
        var profiles = _credentialStore.GetProfiles();
        if (!profiles.ContainsKey(settings.Name))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Profile '{Markup.Escape(settings.Name)}' does not exist.");
            return Task.FromResult(1);
        }

        if (!settings.Yes)
        {
            var confirm = AnsiConsole.Confirm($"Delete profile '{settings.Name}'?", defaultValue: false);
            if (!confirm)
            {
                AnsiConsole.MarkupLine("[yellow]Cancelled.[/]");
                return Task.FromResult(0);
            }
        }

        _credentialStore.DeleteProfile(settings.Name);
        AnsiConsole.MarkupLine($"[green]Profile '{Markup.Escape(settings.Name)}' deleted.[/]");

        var remaining = _credentialStore.GetActiveProfileName();
        if (remaining is not null)
            AnsiConsole.MarkupLine($"[dim]Active profile is now '{Markup.Escape(remaining)}'.[/]");

        return Task.FromResult(0);
    }
}

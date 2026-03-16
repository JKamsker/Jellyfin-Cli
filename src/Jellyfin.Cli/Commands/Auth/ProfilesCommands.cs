using System.ComponentModel;

using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Auth;

// ---------------------------------------------------------------------------
// auth profiles list [--server <host>]
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
        _credentialStore.ConfigPathOverride = settings.ConfigPath
            ?? Environment.GetEnvironmentVariable("JF_CONFIG");

        var hosts = _credentialStore.GetHosts();
        var defaultHostname = _credentialStore.GetDefaultHostname();

        if (hosts.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No profiles configured. Run 'jf auth login' to create one.[/]");
            return Task.FromResult(0);
        }

        // If --server is given, show only that host
        IEnumerable<KeyValuePair<string, HostConfig>> filtered = hosts;
        if (!string.IsNullOrEmpty(settings.Server))
        {
            var resolved = _credentialStore.ResolveHost(settings.Server);
            if (resolved is null)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Host '{Markup.Escape(settings.Server)}' not found.");
                return Task.FromResult(1);
            }
            filtered = new[] { new KeyValuePair<string, HostConfig>(resolved.Value.Hostname, resolved.Value.Host) };
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(filtered.SelectMany(h => h.Value.Profiles.Select(p => new
            {
                host = h.Key,
                baseUrl = h.Value.BaseUrl,
                profile = p.Key,
                username = p.Value.Username,
                auth = DescribeAuth(p.Value),
                isDefaultHost = h.Key == defaultHostname,
                isDefaultProfile = p.Key == h.Value.DefaultProfile,
            })));
            return Task.FromResult(0);
        }

        foreach (var (hostname, host) in filtered)
        {
            var hostLabel = hostname == defaultHostname ? $"{hostname} [dim](default host)[/]" : hostname;
            AnsiConsole.MarkupLine($"[bold]{hostLabel}[/]");
            AnsiConsole.MarkupLine($"  [dim]baseUrl:[/] {Markup.Escape(host.BaseUrl)}");

            foreach (var (profileName, profile) in host.Profiles)
            {
                var isDefault = profileName == host.DefaultProfile;
                var marker = isDefault ? "[green]*[/] " : "  ";
                var defaultTag = isDefault ? " [dim](default)[/]" : "";
                var user = !string.IsNullOrEmpty(profile.Username) ? $" [dim]--[/] {Markup.Escape(profile.Username)}" : "";
                var auth = !string.IsNullOrEmpty(profile.ApiKey) ? " [dim]-- API key[/]" : "";
                var urlOverride = !string.IsNullOrEmpty(profile.BaseUrl) ? $" [dim](baseUrl: {Markup.Escape(profile.BaseUrl)})[/]" : "";
                AnsiConsole.MarkupLine($"  {marker}{Markup.Escape(profileName)}{defaultTag}{user}{auth}{urlOverride}");
            }
            AnsiConsole.WriteLine();
        }

        return Task.FromResult(0);
    }

    private static string DescribeAuth(ProfileConfig p)
    {
        if (!string.IsNullOrEmpty(p.Token)) return "token";
        if (!string.IsNullOrEmpty(p.ApiKey)) return "api-key";
        return "none";
    }
}

// ---------------------------------------------------------------------------
// auth profiles show [--server <host>] [--profile <name>]
// ---------------------------------------------------------------------------

public sealed class ProfilesShowCommand : AsyncCommand<GlobalSettings>
{
    private readonly CredentialStore _credentialStore;

    public ProfilesShowCommand(CredentialStore credentialStore)
    {
        _credentialStore = credentialStore;
    }

    public override Task<int> ExecuteAsync(CommandContext context, GlobalSettings settings, CancellationToken cancellationToken)
    {
        _credentialStore.ConfigPathOverride = settings.ConfigPath
            ?? Environment.GetEnvironmentVariable("JF_CONFIG");

        var resolved = _credentialStore.Resolve(
            settings.Server ?? Environment.GetEnvironmentVariable("JF_SERVER"),
            settings.Profile ?? Environment.GetEnvironmentVariable("JF_PROFILE"));

        if (resolved is null)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Could not resolve a profile. Use --server and/or --profile.");
            return Task.FromResult(1);
        }

        var auth = !string.IsNullOrEmpty(resolved.Token) ? "token"
            : !string.IsNullOrEmpty(resolved.ApiKey) ? "api-key"
            : "none";

        if (settings.Json)
        {
            OutputHelper.WriteJson(new
            {
                host = resolved.Hostname,
                profile = resolved.ProfileName,
                baseUrl = resolved.BaseUrl,
                username = resolved.Username,
                auth,
            });
            return Task.FromResult(0);
        }

        var table = OutputHelper.CreateTable("Field", "Value");
        table.AddRow("Host", resolved.Hostname);
        table.AddRow("Profile", resolved.ProfileName);
        table.AddRow("Base URL", resolved.BaseUrl);
        table.AddRow("Username", resolved.Username ?? "");
        table.AddRow("Auth", auth);
        OutputHelper.WriteTable(table);

        return Task.FromResult(0);
    }
}

// ---------------------------------------------------------------------------
// auth profiles use <name> [--server <host>]
// ---------------------------------------------------------------------------

public sealed class ProfilesUseSettings : GlobalSettings
{
    [CommandArgument(0, "<NAME>")]
    [Description("Profile name to set as default")]
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
        _credentialStore.ConfigPathOverride = settings.ConfigPath
            ?? Environment.GetEnvironmentVariable("JF_CONFIG");

        var hostEntry = _credentialStore.ResolveHost(settings.Server);
        if (hostEntry is null)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Could not resolve host. Use --server to specify.");
            return Task.FromResult(1);
        }

        var (hostname, host) = hostEntry.Value;

        if (!host.Profiles.ContainsKey(settings.Name))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Profile '{Markup.Escape(settings.Name)}' does not exist on host '{Markup.Escape(hostname)}'.");
            if (host.Profiles.Count > 0)
                AnsiConsole.MarkupLine($"[dim]Available:[/] {string.Join(", ", host.Profiles.Keys)}");
            return Task.FromResult(1);
        }

        _credentialStore.SetDefaultProfile(hostname, settings.Name);
        AnsiConsole.MarkupLine($"[green]Default profile for '{Markup.Escape(hostname)}' set to '{Markup.Escape(settings.Name)}'.[/]");

        return Task.FromResult(0);
    }
}

// ---------------------------------------------------------------------------
// auth profiles rename <old> <new> [--server <host>]
// ---------------------------------------------------------------------------

public sealed class ProfilesRenameSettings : GlobalSettings
{
    [CommandArgument(0, "<OLD>")]
    [Description("Current profile name")]
    public string OldName { get; set; } = string.Empty;

    [CommandArgument(1, "<NEW>")]
    [Description("New profile name")]
    public string NewName { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(OldName))
            return ValidationResult.Error("Current profile name is required.");
        if (string.IsNullOrWhiteSpace(NewName))
            return ValidationResult.Error("New profile name is required.");
        return ValidationResult.Success();
    }
}

public sealed class ProfilesRenameCommand : AsyncCommand<ProfilesRenameSettings>
{
    private readonly CredentialStore _credentialStore;

    public ProfilesRenameCommand(CredentialStore credentialStore)
    {
        _credentialStore = credentialStore;
    }

    public override Task<int> ExecuteAsync(CommandContext context, ProfilesRenameSettings settings, CancellationToken cancellationToken)
    {
        _credentialStore.ConfigPathOverride = settings.ConfigPath
            ?? Environment.GetEnvironmentVariable("JF_CONFIG");

        var hostEntry = _credentialStore.ResolveHost(settings.Server);
        if (hostEntry is null)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Could not resolve host. Use --server to specify.");
            return Task.FromResult(1);
        }

        try
        {
            _credentialStore.RenameProfile(hostEntry.Value.Hostname, settings.OldName, settings.NewName);
            AnsiConsole.MarkupLine($"[green]Renamed profile '{Markup.Escape(settings.OldName)}' to '{Markup.Escape(settings.NewName)}' on host '{Markup.Escape(hostEntry.Value.Hostname)}'.[/]");
            return Task.FromResult(0);
        }
        catch (InvalidOperationException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            return Task.FromResult(1);
        }
    }
}

// ---------------------------------------------------------------------------
// auth profiles delete <name> [--server <host>]
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
        _credentialStore.ConfigPathOverride = settings.ConfigPath
            ?? Environment.GetEnvironmentVariable("JF_CONFIG");

        var hostEntry = _credentialStore.ResolveHost(settings.Server);
        if (hostEntry is null)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Could not resolve host. Use --server to specify.");
            return Task.FromResult(1);
        }

        var (hostname, host) = hostEntry.Value;

        if (!host.Profiles.ContainsKey(settings.Name))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Profile '{Markup.Escape(settings.Name)}' does not exist on host '{Markup.Escape(hostname)}'.");
            return Task.FromResult(1);
        }

        if (!settings.Yes)
        {
            var confirm = AnsiConsole.Confirm($"Delete profile '{settings.Name}' from host '{hostname}'?", defaultValue: false);
            if (!confirm)
            {
                AnsiConsole.MarkupLine("[yellow]Cancelled.[/]");
                return Task.FromResult(0);
            }
        }

        _credentialStore.DeleteProfile(hostname, settings.Name);
        AnsiConsole.MarkupLine($"[green]Profile '{Markup.Escape(settings.Name)}' deleted from host '{Markup.Escape(hostname)}'.[/]");

        return Task.FromResult(0);
    }
}

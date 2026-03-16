using System.ComponentModel;

using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Auth;

// ---------------------------------------------------------------------------
// auth host list
// ---------------------------------------------------------------------------

public sealed class HostListCommand : AsyncCommand<GlobalSettings>
{
    private readonly CredentialStore _credentialStore;

    public HostListCommand(CredentialStore credentialStore)
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
            AnsiConsole.MarkupLine("[yellow]No hosts configured. Run 'jf auth login' to add one.[/]");
            return Task.FromResult(0);
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(hosts.Select(h => new
            {
                hostname = h.Key,
                baseUrl = h.Value.BaseUrl,
                aliases = h.Value.Aliases ?? [],
                profiles = h.Value.Profiles.Count,
                isDefault = h.Key == defaultHostname,
            }));
            return Task.FromResult(0);
        }

        var table = OutputHelper.CreateTable("", "Hostname", "Base URL", "Aliases", "Profiles");
        foreach (var (hostname, host) in hosts)
        {
            var marker = hostname == defaultHostname ? "[green]*[/]" : "";
            var aliases = host.Aliases is { Count: > 0 } ? string.Join(", ", host.Aliases) : "";
            table.AddRow(marker, Markup.Escape(hostname), Markup.Escape(host.BaseUrl), Markup.Escape(aliases), host.Profiles.Count.ToString());
        }
        OutputHelper.WriteTable(table);

        return Task.FromResult(0);
    }
}

// ---------------------------------------------------------------------------
// auth host use <hostname>
// ---------------------------------------------------------------------------

public sealed class HostUseSettings : GlobalSettings
{
    [CommandArgument(0, "<HOSTNAME>")]
    [Description("Hostname to set as default")]
    public string Hostname { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Hostname))
            return ValidationResult.Error("Hostname is required.");
        return ValidationResult.Success();
    }
}

public sealed class HostUseCommand : AsyncCommand<HostUseSettings>
{
    private readonly CredentialStore _credentialStore;

    public HostUseCommand(CredentialStore credentialStore)
    {
        _credentialStore = credentialStore;
    }

    public override Task<int> ExecuteAsync(CommandContext context, HostUseSettings settings, CancellationToken cancellationToken)
    {
        _credentialStore.ConfigPathOverride = settings.ConfigPath
            ?? Environment.GetEnvironmentVariable("JF_CONFIG");

        try
        {
            _credentialStore.SetDefaultHost(settings.Hostname.ToLowerInvariant());
            AnsiConsole.MarkupLine($"[green]Default host set to '{Markup.Escape(settings.Hostname)}'.[/]");
            return Task.FromResult(0);
        }
        catch (InvalidOperationException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            var hosts = _credentialStore.GetHosts();
            if (hosts.Count > 0)
                AnsiConsole.MarkupLine($"[dim]Available:[/] {string.Join(", ", hosts.Keys)}");
            return Task.FromResult(1);
        }
    }
}

// ---------------------------------------------------------------------------
// auth host rename <old> <new>
// ---------------------------------------------------------------------------

public sealed class HostRenameSettings : GlobalSettings
{
    [CommandArgument(0, "<OLD>")]
    [Description("Current hostname key")]
    public string OldName { get; set; } = string.Empty;

    [CommandArgument(1, "<NEW>")]
    [Description("New hostname key")]
    public string NewName { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(OldName))
            return ValidationResult.Error("Current hostname is required.");
        if (string.IsNullOrWhiteSpace(NewName))
            return ValidationResult.Error("New hostname is required.");
        return ValidationResult.Success();
    }
}

public sealed class HostRenameCommand : AsyncCommand<HostRenameSettings>
{
    private readonly CredentialStore _credentialStore;

    public HostRenameCommand(CredentialStore credentialStore)
    {
        _credentialStore = credentialStore;
    }

    public override Task<int> ExecuteAsync(CommandContext context, HostRenameSettings settings, CancellationToken cancellationToken)
    {
        _credentialStore.ConfigPathOverride = settings.ConfigPath
            ?? Environment.GetEnvironmentVariable("JF_CONFIG");

        try
        {
            _credentialStore.RenameHost(settings.OldName.ToLowerInvariant(), settings.NewName);
            AnsiConsole.MarkupLine($"[green]Renamed host '{Markup.Escape(settings.OldName)}' to '{Markup.Escape(settings.NewName)}'.[/]");
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
// auth host delete <hostname>
// ---------------------------------------------------------------------------

public sealed class HostDeleteSettings : GlobalSettings
{
    [CommandArgument(0, "<HOSTNAME>")]
    [Description("Hostname to remove")]
    public string Hostname { get; set; } = string.Empty;

    [CommandOption("-f|--force")]
    [Description("Skip confirmation even if host has multiple profiles")]
    public bool Force { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Hostname))
            return ValidationResult.Error("Hostname is required.");
        return ValidationResult.Success();
    }
}

public sealed class HostDeleteCommand : AsyncCommand<HostDeleteSettings>
{
    private readonly CredentialStore _credentialStore;

    public HostDeleteCommand(CredentialStore credentialStore)
    {
        _credentialStore = credentialStore;
    }

    public override Task<int> ExecuteAsync(CommandContext context, HostDeleteSettings settings, CancellationToken cancellationToken)
    {
        _credentialStore.ConfigPathOverride = settings.ConfigPath
            ?? Environment.GetEnvironmentVariable("JF_CONFIG");

        var hostname = settings.Hostname.ToLowerInvariant();
        var hosts = _credentialStore.GetHosts();

        if (!hosts.TryGetValue(hostname, out var host))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Host '{Markup.Escape(hostname)}' does not exist.");
            return Task.FromResult(1);
        }

        if (!settings.Force && !settings.Yes && host.Profiles.Count > 1)
        {
            var confirm = AnsiConsole.Confirm(
                $"Host '{hostname}' has {host.Profiles.Count} profiles. Delete all?", defaultValue: false);
            if (!confirm)
            {
                AnsiConsole.MarkupLine("[yellow]Cancelled.[/]");
                return Task.FromResult(0);
            }
        }

        _credentialStore.DeleteHost(hostname);
        AnsiConsole.MarkupLine($"[green]Host '{Markup.Escape(hostname)}' and all its profiles removed.[/]");

        return Task.FromResult(0);
    }
}

// ---------------------------------------------------------------------------
// auth host alias add <hostname> <alias>
// ---------------------------------------------------------------------------

public sealed class HostAliasAddSettings : GlobalSettings
{
    [CommandArgument(0, "<HOSTNAME>")]
    [Description("Host to add the alias to")]
    public string Hostname { get; set; } = string.Empty;

    [CommandArgument(1, "<ALIAS>")]
    [Description("Short alias for the host")]
    public string Alias { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Hostname))
            return ValidationResult.Error("Hostname is required.");
        if (string.IsNullOrWhiteSpace(Alias))
            return ValidationResult.Error("Alias is required.");
        return ValidationResult.Success();
    }
}

public sealed class HostAliasAddCommand : AsyncCommand<HostAliasAddSettings>
{
    private readonly CredentialStore _credentialStore;

    public HostAliasAddCommand(CredentialStore credentialStore)
    {
        _credentialStore = credentialStore;
    }

    public override Task<int> ExecuteAsync(CommandContext context, HostAliasAddSettings settings, CancellationToken cancellationToken)
    {
        _credentialStore.ConfigPathOverride = settings.ConfigPath
            ?? Environment.GetEnvironmentVariable("JF_CONFIG");

        try
        {
            var conflict = _credentialStore.AddAlias(settings.Hostname.ToLowerInvariant(), settings.Alias);
            AnsiConsole.MarkupLine($"[green]Alias '{Markup.Escape(settings.Alias)}' added to host '{Markup.Escape(settings.Hostname)}'.[/]");
            if (conflict is not null)
                AnsiConsole.MarkupLine($"[yellow]Warning:[/] Host '{Markup.Escape(conflict)}' also has this alias. Using --server '{Markup.Escape(settings.Alias)}' will resolve to the first match.");
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
// auth host alias remove <hostname> <alias>
// ---------------------------------------------------------------------------

public sealed class HostAliasRemoveSettings : GlobalSettings
{
    [CommandArgument(0, "<HOSTNAME>")]
    [Description("Host to remove the alias from")]
    public string Hostname { get; set; } = string.Empty;

    [CommandArgument(1, "<ALIAS>")]
    [Description("Alias to remove")]
    public string Alias { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Hostname))
            return ValidationResult.Error("Hostname is required.");
        if (string.IsNullOrWhiteSpace(Alias))
            return ValidationResult.Error("Alias is required.");
        return ValidationResult.Success();
    }
}

public sealed class HostAliasRemoveCommand : AsyncCommand<HostAliasRemoveSettings>
{
    private readonly CredentialStore _credentialStore;

    public HostAliasRemoveCommand(CredentialStore credentialStore)
    {
        _credentialStore = credentialStore;
    }

    public override Task<int> ExecuteAsync(CommandContext context, HostAliasRemoveSettings settings, CancellationToken cancellationToken)
    {
        _credentialStore.ConfigPathOverride = settings.ConfigPath
            ?? Environment.GetEnvironmentVariable("JF_CONFIG");

        try
        {
            _credentialStore.RemoveAlias(settings.Hostname.ToLowerInvariant(), settings.Alias);
            AnsiConsole.MarkupLine($"[green]Alias '{Markup.Escape(settings.Alias)}' removed from host '{Markup.Escape(settings.Hostname)}'.[/]");
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
// auth host alias list [<hostname>]
// ---------------------------------------------------------------------------

public sealed class HostAliasListSettings : GlobalSettings
{
    [CommandArgument(0, "[HOSTNAME]")]
    [Description("Show aliases for a specific host (default: all hosts)")]
    public string? Hostname { get; set; }
}

public sealed class HostAliasListCommand : AsyncCommand<HostAliasListSettings>
{
    private readonly CredentialStore _credentialStore;

    public HostAliasListCommand(CredentialStore credentialStore)
    {
        _credentialStore = credentialStore;
    }

    public override Task<int> ExecuteAsync(CommandContext context, HostAliasListSettings settings, CancellationToken cancellationToken)
    {
        _credentialStore.ConfigPathOverride = settings.ConfigPath
            ?? Environment.GetEnvironmentVariable("JF_CONFIG");

        var hosts = _credentialStore.GetHosts();

        IEnumerable<KeyValuePair<string, HostConfig>> filtered = hosts;
        if (!string.IsNullOrEmpty(settings.Hostname))
        {
            var key = settings.Hostname.ToLowerInvariant();
            if (!hosts.TryGetValue(key, out var host))
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Host '{Markup.Escape(key)}' not found.");
                return Task.FromResult(1);
            }
            filtered = new[] { new KeyValuePair<string, HostConfig>(key, host) };
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(filtered.Select(h => new
            {
                hostname = h.Key,
                aliases = h.Value.Aliases ?? [],
            }));
            return Task.FromResult(0);
        }

        var any = false;
        foreach (var (hostname, host) in filtered)
        {
            if (host.Aliases is not { Count: > 0 })
                continue;
            any = true;
            AnsiConsole.MarkupLine($"[bold]{Markup.Escape(hostname)}[/]: {Markup.Escape(string.Join(", ", host.Aliases))}");
        }

        if (!any)
            AnsiConsole.MarkupLine("[dim]No aliases configured.[/]");

        return Task.FromResult(0);
    }
}

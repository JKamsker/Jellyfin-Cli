using System.ComponentModel;

using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Auth;

public sealed class HostDeleteSettings : GlobalSettings
{
    [CommandArgument(0, "<HOSTNAME>")]
    [Description("Hostname to remove")]
    public string Hostname { get; set; } = string.Empty;

    [CommandOption("-f|--force")]
    [Description("Skip deletion confirmation")]
    public bool Force { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Hostname?.Trim()))
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

        var hostname = settings.Hostname.Trim().ToLowerInvariant();
        var hosts = _credentialStore.GetHosts();

        if (!hosts.TryGetValue(hostname, out var host))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Host '{Markup.Escape(hostname)}' does not exist.");
            return Task.FromResult(1);
        }

        if (!settings.Force && !settings.Yes && host.Profiles.Count > 0)
        {
            var profileLabel = host.Profiles.Count == 1 ? "1 profile" : $"{host.Profiles.Count} profiles";
            var confirm = AnsiConsole.Confirm(
                $"Host '{Markup.Escape(hostname)}' has {profileLabel}. Delete?", defaultValue: false);
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

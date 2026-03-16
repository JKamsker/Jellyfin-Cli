using System.ComponentModel;

using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Auth;

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

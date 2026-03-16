using System.ComponentModel;

using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Auth;

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

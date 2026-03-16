using System.ComponentModel;

using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Auth;

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

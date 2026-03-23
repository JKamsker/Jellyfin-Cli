using System.ComponentModel;

using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Auth;

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

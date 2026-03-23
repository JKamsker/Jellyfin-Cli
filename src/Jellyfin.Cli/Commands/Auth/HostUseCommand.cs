using System.ComponentModel;

using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Auth;

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

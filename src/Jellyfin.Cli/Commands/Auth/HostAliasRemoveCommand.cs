using System.ComponentModel;

using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Auth;

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

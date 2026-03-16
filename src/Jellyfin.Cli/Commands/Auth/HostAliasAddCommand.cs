using System.ComponentModel;

using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Auth;

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

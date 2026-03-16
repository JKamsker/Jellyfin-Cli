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
            var hostname = settings.Hostname.Trim().ToLowerInvariant();
            var alias = settings.Alias.Trim().ToLowerInvariant();

            var added = _credentialStore.AddAlias(hostname, alias, out var conflict);
            if (added)
                AnsiConsole.MarkupLine($"[green]Alias '{Markup.Escape(alias)}' added to host '{Markup.Escape(hostname)}'.[/]");
            else
                AnsiConsole.MarkupLine($"[yellow]Alias '{Markup.Escape(alias)}' already exists on host '{Markup.Escape(hostname)}'.[/]");
            if (conflict is not null)
                AnsiConsole.MarkupLine($"[yellow]Warning:[/] Host '{Markup.Escape(conflict)}' also has this alias. Using --server '{Markup.Escape(alias)}' will resolve to the first match.");
            return Task.FromResult(0);
        }
        catch (InvalidOperationException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            return Task.FromResult(1);
        }
    }
}

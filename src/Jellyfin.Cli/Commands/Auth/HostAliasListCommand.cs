using System.ComponentModel;

using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Auth;

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

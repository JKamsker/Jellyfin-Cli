using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Auth;

public sealed class HostListCommand : AsyncCommand<GlobalSettings>
{
    private readonly CredentialStore _credentialStore;

    public HostListCommand(CredentialStore credentialStore)
    {
        _credentialStore = credentialStore;
    }

    public override Task<int> ExecuteAsync(CommandContext context, GlobalSettings settings, CancellationToken cancellationToken)
    {
        _credentialStore.ConfigPathOverride = settings.ConfigPath
            ?? Environment.GetEnvironmentVariable("JF_CONFIG");

        var hosts = _credentialStore.GetHosts();
        var defaultHostname = _credentialStore.GetDefaultHostname();

        if (hosts.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No hosts configured. Run 'jf auth login' to add one.[/]");
            return Task.FromResult(0);
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(hosts.Select(h => new
            {
                hostname = h.Key,
                baseUrl = h.Value.BaseUrl,
                aliases = h.Value.Aliases ?? [],
                profiles = h.Value.Profiles.Count,
                isDefault = h.Key == defaultHostname,
            }));
            return Task.FromResult(0);
        }

        var table = OutputHelper.CreateTable("", "Hostname", "Base URL", "Aliases", "Profiles");
        foreach (var (hostname, host) in hosts)
        {
            var marker = hostname == defaultHostname ? "[green]*[/]" : "";
            var aliases = host.Aliases is { Count: > 0 } ? string.Join(", ", host.Aliases) : "";
            table.AddRow(marker, Markup.Escape(hostname), Markup.Escape(host.BaseUrl), Markup.Escape(aliases), host.Profiles.Count.ToString());
        }
        OutputHelper.WriteTable(table);

        return Task.FromResult(0);
    }
}

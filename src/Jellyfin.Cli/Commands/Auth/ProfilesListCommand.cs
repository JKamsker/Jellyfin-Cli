using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Auth;

public sealed class ProfilesListCommand : AsyncCommand<GlobalSettings>
{
    private readonly CredentialStore _credentialStore;

    public ProfilesListCommand(CredentialStore credentialStore)
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
            AnsiConsole.MarkupLine("[yellow]No profiles configured. Run 'jf auth login' to create one.[/]");
            return Task.FromResult(0);
        }

        // If --server is given, show only that host
        IEnumerable<KeyValuePair<string, HostConfig>> filtered = hosts;
        if (!string.IsNullOrEmpty(settings.Server))
        {
            var resolved = _credentialStore.ResolveHost(settings.Server);
            if (resolved is null)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Host '{Markup.Escape(settings.Server)}' not found.");
                return Task.FromResult(1);
            }
            filtered = new[] { new KeyValuePair<string, HostConfig>(resolved.Value.Hostname, resolved.Value.Host) };
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(filtered.SelectMany(h => h.Value.Profiles.Select(p => new
            {
                host = h.Key,
                baseUrl = h.Value.BaseUrl,
                profile = p.Key,
                username = p.Value.Username,
                auth = DescribeAuth(p.Value),
                isDefaultHost = h.Key == defaultHostname,
                isDefaultProfile = p.Key == h.Value.DefaultProfile,
            })));
            return Task.FromResult(0);
        }

        foreach (var (hostname, host) in filtered)
        {
            var hostLabel = hostname == defaultHostname ? $"{hostname} [dim](default host)[/]" : hostname;
            AnsiConsole.MarkupLine($"[bold]{hostLabel}[/]");
            AnsiConsole.MarkupLine($"  [dim]baseUrl:[/] {Markup.Escape(host.BaseUrl)}");

            foreach (var (profileName, profile) in host.Profiles)
            {
                var isDefault = profileName == host.DefaultProfile;
                var marker = isDefault ? "[green]*[/] " : "  ";
                var defaultTag = isDefault ? " [dim](default)[/]" : "";
                var user = !string.IsNullOrEmpty(profile.Username) ? $" [dim]--[/] {Markup.Escape(profile.Username)}" : "";
                var auth = !string.IsNullOrEmpty(profile.ApiKey) ? " [dim]-- API key[/]" : "";
                var urlOverride = !string.IsNullOrEmpty(profile.BaseUrl) ? $" [dim](baseUrl: {Markup.Escape(profile.BaseUrl)})[/]" : "";
                AnsiConsole.MarkupLine($"  {marker}{Markup.Escape(profileName)}{defaultTag}{user}{auth}{urlOverride}");
            }
            AnsiConsole.WriteLine();
        }

        return Task.FromResult(0);
    }

    private static string DescribeAuth(ProfileConfig p)
    {
        if (!string.IsNullOrEmpty(p.Token)) return "token";
        if (!string.IsNullOrEmpty(p.ApiKey)) return "api-key";
        return "none";
    }
}

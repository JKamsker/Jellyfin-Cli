using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Auth;

public sealed class ProfilesShowCommand : AsyncCommand<GlobalSettings>
{
    private readonly CredentialStore _credentialStore;

    public ProfilesShowCommand(CredentialStore credentialStore)
    {
        _credentialStore = credentialStore;
    }

    public override Task<int> ExecuteAsync(CommandContext context, GlobalSettings settings, CancellationToken cancellationToken)
    {
        _credentialStore.ConfigPathOverride = settings.ConfigPath
            ?? Environment.GetEnvironmentVariable("JF_CONFIG");

        var resolved = _credentialStore.Resolve(
            settings.Server ?? Environment.GetEnvironmentVariable("JF_SERVER"),
            settings.Profile ?? Environment.GetEnvironmentVariable("JF_PROFILE"));

        if (resolved is null)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Could not resolve a profile. Use --server and/or --profile.");
            return Task.FromResult(1);
        }

        var auth = !string.IsNullOrEmpty(resolved.Token) ? "token"
            : !string.IsNullOrEmpty(resolved.ApiKey) ? "api-key"
            : "none";

        if (settings.Json)
        {
            OutputHelper.WriteJson(new
            {
                host = resolved.Hostname,
                profile = resolved.ProfileName,
                baseUrl = resolved.BaseUrl,
                username = resolved.Username,
                auth,
            });
            return Task.FromResult(0);
        }

        var table = OutputHelper.CreateTable("Field", "Value");
        table.AddRow("Host", resolved.Hostname);
        table.AddRow("Profile", resolved.ProfileName);
        table.AddRow("Base URL", resolved.BaseUrl);
        table.AddRow("Username", resolved.Username ?? "");
        table.AddRow("Auth", auth);
        OutputHelper.WriteTable(table);

        return Task.FromResult(0);
    }
}

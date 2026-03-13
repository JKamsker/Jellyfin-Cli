using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Auth;

public sealed class WhoAmICommand : ApiCommand<GlobalSettings>
{
    private readonly CredentialStore _credentialStore;

    public WhoAmICommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
        _credentialStore = credentialStore;
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, GlobalSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var user = await client.Users.Me.GetAsync();

        if (user is null)
        {
            AnsiConsole.MarkupLine("[red]Could not retrieve current user.[/]");
            return 1;
        }

        var stored = _credentialStore.Load();
        var serverUrl = settings.Server ?? stored?.Server ?? "(unknown)";

        if (settings.Json)
        {
            OutputHelper.WriteJson(new
            {
                name = user.Name,
                id = user.Id,
                isAdministrator = user.Policy?.IsAdministrator,
                server = serverUrl,
            });
            return 0;
        }

        var table = OutputHelper.CreateTable("Field", "Value");
        table.AddRow("Name", user.Name ?? "(unknown)");
        table.AddRow("ID", user.Id?.ToString() ?? "(unknown)");
        table.AddRow("Administrator", user.Policy?.IsAdministrator == true ? "Yes" : "No");
        table.AddRow("Server", serverUrl);
        OutputHelper.WriteTable(table);

        return 0;
    }
}

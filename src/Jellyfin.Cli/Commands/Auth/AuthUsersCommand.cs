using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Auth;

public sealed class AuthUsersCommand : ApiCommand<GlobalSettings>
{
    public AuthUsersCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, GlobalSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var users = await client.Users.Public.GetAsync();

        if (users is null || users.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No public users found.[/]");
            return 0;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(users.Select(u => new
            {
                name = u.Name,
                id = u.Id,
                hasPassword = u.HasPassword,
            }));
            return 0;
        }

        var table = OutputHelper.CreateTable("Name", "ID", "HasPassword");
        foreach (var user in users)
        {
            table.AddRow(
                user.Name ?? "(unknown)",
                user.Id?.ToString() ?? "(unknown)",
                user.HasPassword == true ? "Yes" : "No");
        }

        OutputHelper.WriteTable(table);
        return 0;
    }
}

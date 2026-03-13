using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Users;

public sealed class UsersListSettings : GlobalSettings
{
}

public sealed class UsersListCommand : ApiCommand<UsersListSettings>
{
    public UsersListCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, UsersListSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var users = await client.Users.GetAsync();

        if (users is null || users.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No users found.[/]");
            return 0;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(users);
            return 0;
        }

        var table = OutputHelper.CreateTable("Id", "Name", "IsAdmin", "IsDisabled", "LastLogin");

        foreach (var user in users)
        {
            table.AddRow(
                user.Id?.ToString() ?? "",
                user.Name ?? "",
                user.Policy?.IsAdministrator?.ToString() ?? "",
                user.Policy?.IsDisabled?.ToString() ?? "",
                user.LastLoginDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never"
            );
        }

        OutputHelper.WriteTable(table);
        return 0;
    }
}

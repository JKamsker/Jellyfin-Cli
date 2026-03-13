using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Users;

public sealed class UsersDeleteSettings : GlobalSettings
{
    [CommandArgument(0, "<USER_ID>")]
    [Description("The user ID to delete")]
    public string UserId { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(UserId))
            return ValidationResult.Error("USER_ID is required.");

        if (!Guid.TryParse(UserId, out _))
            return ValidationResult.Error("USER_ID must be a valid GUID.");

        return ValidationResult.Success();
    }
}

public sealed class UsersDeleteCommand : ApiCommand<UsersDeleteSettings>
{
    public UsersDeleteCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, UsersDeleteSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(settings.UserId);

        if (!settings.Yes)
        {
            var confirmed = OutputHelper.Confirm(
                $"Are you sure you want to delete user [bold]{userId}[/]?");

            if (!confirmed)
            {
                AnsiConsole.MarkupLine("[yellow]Aborted.[/]");
                return 0;
            }
        }

        await client.Users[userId].DeleteAsync();

        AnsiConsole.MarkupLine("[green]User deleted successfully.[/]");
        return 0;
    }
}

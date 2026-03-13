using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Api.Generated.Models;
using Jellyfin.Cli.Common;

using Microsoft.Kiota.Abstractions;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Users;

public sealed class UsersUpdateSettings : GlobalSettings
{
    [CommandArgument(0, "<USER_ID>")]
    [Description("The user ID to update")]
    public string UserId { get; set; } = string.Empty;

    [CommandOption("-n|--name <NAME>")]
    [Description("New display name for the user")]
    public string? Name { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(UserId))
            return ValidationResult.Error("USER_ID is required.");

        if (!Guid.TryParse(UserId, out _))
            return ValidationResult.Error("USER_ID must be a valid GUID.");

        if (string.IsNullOrWhiteSpace(Name))
            return ValidationResult.Error("At least --name must be specified.");

        return ValidationResult.Success();
    }
}

public sealed class UsersUpdateCommand : ApiCommand<UsersUpdateSettings>
{
    public UsersUpdateCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, UsersUpdateSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(settings.UserId);

        // Fetch current user data first
        var user = await client.Users[userId].GetAsync();
        if (user is null)
        {
            AnsiConsole.MarkupLine("[red]User not found.[/]");
            return 1;
        }

        if (!string.IsNullOrWhiteSpace(settings.Name))
        {
            user.Name = settings.Name;
        }

        await client.Users.PostAsync(user, config =>
        {
            config.QueryParameters.UserId = userId;
        });

        AnsiConsole.MarkupLine($"[green]User updated successfully.[/]");
        return 0;
    }
}

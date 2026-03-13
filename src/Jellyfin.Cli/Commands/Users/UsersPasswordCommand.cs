using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Api.Generated.Models;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Users;

public sealed class UsersPasswordSettings : GlobalSettings
{
    [CommandArgument(0, "<USER_ID>")]
    [Description("The user ID whose password to change")]
    public string UserId { get; set; } = string.Empty;

    [CommandOption("--new-password <PASSWORD>")]
    [Description("New password (prompted if omitted)")]
    public string? NewPassword { get; set; }

    [CommandOption("--current-password <PASSWORD>")]
    [Description("Current password (required when changing your own password)")]
    public string? CurrentPassword { get; set; }

    [CommandOption("--reset")]
    [Description("Reset the password instead of setting a new one")]
    public bool Reset { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(UserId))
            return ValidationResult.Error("USER_ID is required.");

        if (!Guid.TryParse(UserId, out _))
            return ValidationResult.Error("USER_ID must be a valid GUID.");

        return ValidationResult.Success();
    }
}

public sealed class UsersPasswordCommand : ApiCommand<UsersPasswordSettings>
{
    public UsersPasswordCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, UsersPasswordSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(settings.UserId);

        var body = new UpdateUserPassword();

        if (settings.Reset)
        {
            body.ResetPassword = true;
        }
        else
        {
            var newPassword = settings.NewPassword;
            if (string.IsNullOrEmpty(newPassword))
            {
                newPassword = AnsiConsole.Prompt(
                    new TextPrompt<string>("New password:")
                        .Secret());
            }

            body.NewPw = newPassword;
            body.CurrentPw = settings.CurrentPassword;
        }

        await client.Users.Password.PostAsync(body, config =>
        {
            config.QueryParameters.UserId = userId;
        });

        var message = settings.Reset
            ? "[green]Password reset successfully.[/]"
            : "[green]Password changed successfully.[/]";

        AnsiConsole.MarkupLine(message);
        return 0;
    }
}

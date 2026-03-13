using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Users;

public sealed class UsersGetSettings : GlobalSettings
{
    [CommandArgument(0, "<USER_ID>")]
    [Description("The user ID to retrieve")]
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

public sealed class UsersGetCommand : ApiCommand<UsersGetSettings>
{
    public UsersGetCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, UsersGetSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(settings.UserId);
        var user = await client.Users[userId].GetAsync();

        if (user is null)
        {
            AnsiConsole.MarkupLine("[red]User not found.[/]");
            return 1;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(user);
            return 0;
        }

        var table = OutputHelper.CreateTable("Field", "Value");
        table.AddRow("Id", user.Id?.ToString() ?? "");
        table.AddRow("Name", user.Name ?? "");
        table.AddRow("ServerId", user.ServerId ?? "");
        table.AddRow("ServerName", user.ServerName ?? "");
        table.AddRow("IsAdmin", user.Policy?.IsAdministrator?.ToString() ?? "");
        table.AddRow("IsDisabled", user.Policy?.IsDisabled?.ToString() ?? "");
        table.AddRow("IsHidden", user.Policy?.IsHidden?.ToString() ?? "");
        table.AddRow("HasPassword", user.HasPassword?.ToString() ?? "");
        table.AddRow("EnableAutoLogin", user.EnableAutoLogin?.ToString() ?? "");
        table.AddRow("LastLoginDate", user.LastLoginDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never");
        table.AddRow("LastActivityDate", user.LastActivityDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never");

        OutputHelper.WriteTable(table);
        return 0;
    }
}

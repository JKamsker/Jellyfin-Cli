using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Api.Generated.Models;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Users;

public sealed class UsersPolicySettings : GlobalSettings
{
    [CommandArgument(0, "<USER_ID>")]
    [Description("The user ID whose policy to update")]
    public string UserId { get; set; } = string.Empty;

    [CommandOption("--admin <BOOL>")]
    [Description("Set administrator status (true/false)")]
    public bool? Admin { get; set; }

    [CommandOption("--disabled <BOOL>")]
    [Description("Set disabled status (true/false)")]
    public bool? Disabled { get; set; }

    [CommandOption("--hidden <BOOL>")]
    [Description("Set hidden status (true/false)")]
    public bool? Hidden { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(UserId))
            return ValidationResult.Error("USER_ID is required.");

        if (!Guid.TryParse(UserId, out _))
            return ValidationResult.Error("USER_ID must be a valid GUID.");

        if (Admin is null && Disabled is null && Hidden is null)
            return ValidationResult.Error("At least one of --admin, --disabled, or --hidden must be specified.");

        return ValidationResult.Success();
    }
}

public sealed class UsersPolicyCommand : ApiCommand<UsersPolicySettings>
{
    public UsersPolicyCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, UsersPolicySettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(settings.UserId);

        // Fetch current user to get existing policy
        var user = await client.Users[userId].GetAsync();
        if (user is null)
        {
            AnsiConsole.MarkupLine("[red]User not found.[/]");
            return 1;
        }

        var policy = user.Policy ?? new UserPolicy();

        if (settings.Admin is not null)
            policy.IsAdministrator = settings.Admin.Value;

        if (settings.Disabled is not null)
            policy.IsDisabled = settings.Disabled.Value;

        if (settings.Hidden is not null)
            policy.IsHidden = settings.Hidden.Value;

        await client.Users[userId].Policy.PostAsync(policy);

        AnsiConsole.MarkupLine("[green]User policy updated successfully.[/]");

        var table = OutputHelper.CreateTable("Field", "Value");
        table.AddRow("UserId", userId.ToString());
        table.AddRow("IsAdministrator", policy.IsAdministrator?.ToString() ?? "");
        table.AddRow("IsDisabled", policy.IsDisabled?.ToString() ?? "");
        table.AddRow("IsHidden", policy.IsHidden?.ToString() ?? "");

        OutputHelper.WriteTable(table);
        return 0;
    }
}

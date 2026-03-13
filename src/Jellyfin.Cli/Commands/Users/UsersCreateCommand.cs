using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Api.Generated.Models;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Users;

public sealed class UsersCreateSettings : GlobalSettings
{
    [CommandArgument(0, "<NAME>")]
    [Description("Username for the new user")]
    public string Name { get; set; } = string.Empty;

    [CommandOption("-p|--password <PASSWORD>")]
    [Description("Password for the new user (optional)")]
    public string? Password { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return ValidationResult.Error("NAME is required.");

        return ValidationResult.Success();
    }
}

public sealed class UsersCreateCommand : ApiCommand<UsersCreateSettings>
{
    public UsersCreateCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, UsersCreateSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var body = new CreateUserByName
        {
            Name = settings.Name,
            Password = settings.Password,
        };

        var user = await client.Users.New.PostAsync(body);

        if (user is null)
        {
            AnsiConsole.MarkupLine("[red]Failed to create user.[/]");
            return 1;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(user);
            return 0;
        }

        AnsiConsole.MarkupLine($"[green]User created successfully.[/]");

        var table = OutputHelper.CreateTable("Field", "Value");
        table.AddRow("Id", user.Id?.ToString() ?? "");
        table.AddRow("Name", user.Name ?? "");

        OutputHelper.WriteTable(table);
        return 0;
    }
}

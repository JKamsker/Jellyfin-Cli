using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Api.Generated.Models;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Sessions;

public sealed class SessionsCommandSendSettings : GlobalSettings
{
    [CommandArgument(0, "<SESSION_ID>")]
    [Description("Target session id")]
    public string SessionId { get; set; } = string.Empty;

    [CommandArgument(1, "<COMMAND>")]
    [Description("General command name (e.g. MoveUp, GoHome, ToggleMute, SetVolume, DisplayMessage)")]
    public string CommandName { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(SessionId))
            return ValidationResult.Error("SESSION_ID is required.");

        if (string.IsNullOrWhiteSpace(CommandName))
            return ValidationResult.Error("COMMAND is required.");

        return ValidationResult.Success();
    }
}

public sealed class SessionsCommandSendCommand : ApiCommand<SessionsCommandSendSettings>
{
    public SessionsCommandSendCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, SessionsCommandSendSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        if (!TryParseCommandName(settings.CommandName, out var commandName))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Unknown command '{settings.CommandName}'.");
            AnsiConsole.MarkupLine("[dim]Valid commands:[/] " + string.Join(", ",
                Enum.GetNames<GeneralCommand_Name>()));
            return 1;
        }

        var body = new GeneralCommand
        {
            Name = commandName,
        };

        await client.Sessions[settings.SessionId].Command.PostAsync(body);

        AnsiConsole.MarkupLine($"[green]Command '{settings.CommandName}' sent.[/]");
        return 0;
    }

    private static bool TryParseCommandName(string raw, out GeneralCommand_Name result)
    {
        return Enum.TryParse(raw, ignoreCase: true, out result);
    }
}

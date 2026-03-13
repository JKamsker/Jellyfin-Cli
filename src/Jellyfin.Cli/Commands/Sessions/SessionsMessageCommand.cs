using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Api.Generated.Models;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Sessions;

public sealed class SessionsMessageSettings : GlobalSettings
{
    [CommandArgument(0, "<SESSION_ID>")]
    [Description("Target session id")]
    public string SessionId { get; set; } = string.Empty;

    [CommandArgument(1, "<TEXT>")]
    [Description("Message text to display")]
    public string Text { get; set; } = string.Empty;

    [CommandOption("--header <HEADER>")]
    [Description("Optional message header/title")]
    public string? Header { get; set; }

    [CommandOption("--timeout <MS>")]
    [Description("Display timeout in milliseconds")]
    public long? Timeout { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(SessionId))
            return ValidationResult.Error("SESSION_ID is required.");

        if (string.IsNullOrWhiteSpace(Text))
            return ValidationResult.Error("TEXT is required.");

        return ValidationResult.Success();
    }
}

public sealed class SessionsMessageCommand : ApiCommand<SessionsMessageSettings>
{
    public SessionsMessageCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, SessionsMessageSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var body = new MessageCommand
        {
            Text = settings.Text,
            Header = settings.Header,
            TimeoutMs = settings.Timeout,
        };

        await client.Sessions[settings.SessionId].Message.PostAsync(body);

        AnsiConsole.MarkupLine("[green]Message sent.[/]");
        return 0;
    }
}

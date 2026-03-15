using System.ComponentModel;
using System.Text;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Server;

public sealed class ServerConfigSetSettings : GlobalSettings
{
    [CommandArgument(0, "<KEY>")]
    [Description("Named configuration key")]
    public string Key { get; set; } = string.Empty;

    [CommandOption("--file <FILE>")]
    [Description("Read replacement JSON from a file")]
    public string? FilePath { get; set; }

    [CommandOption("--data <JSON>")]
    [Description("Inline JSON payload")]
    public string? InlineJson { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Key))
            return ValidationResult.Error("A configuration key is required.");

        var hasFile = !string.IsNullOrWhiteSpace(FilePath);
        var hasInline = !string.IsNullOrWhiteSpace(InlineJson);
        return hasFile == hasInline
            ? ValidationResult.Error("Specify exactly one of --file or --data.")
            : ValidationResult.Success();
    }
}

public sealed class ServerConfigSetCommand : ApiCommand<ServerConfigSetSettings>
{
    public ServerConfigSetCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ServerConfigSetSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var payload = JsonCommandHelper.ReadTextFromFileOrInline(settings.FilePath, settings.InlineJson, "--file/--data");

        using var httpClient = CreateHttpClient();
        using var response = await httpClient.PostAsync(
            $"System/Configuration/{Uri.EscapeDataString(settings.Key)}",
            new StringContent(payload, Encoding.UTF8, "application/json"),
            cancellationToken);

        response.EnsureSuccessStatusCode();
        AnsiConsole.MarkupLine($"[green]Updated server configuration section [white]{Markup.Escape(settings.Key)}[/].[/]");
        return 0;
    }
}

using System.ComponentModel;
using System.Text;
using System.Text.Json;

using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Server;

public sealed class ServerConfigMetadataSettings : GlobalSettings
{
    [CommandArgument(0, "[ACTION]")]
    [Description("Optional action. Use 'set' to update the metadata config.")]
    public string? Action { get; set; }

    [CommandOption("--use-file-creation-time-for-date-added <VALUE>")]
    [Description("Set Jellyfin's UseFileCreationTimeForDateAdded metadata option")]
    public string? UseFileCreationTimeForDateAdded { get; set; }

    public override ValidationResult Validate()
    {
        if (Action is not null && !string.Equals(Action, "set", StringComparison.OrdinalIgnoreCase))
            return ValidationResult.Error("Only the 'set' action is supported.");

        if (string.Equals(Action, "set", StringComparison.OrdinalIgnoreCase) &&
            !bool.TryParse(UseFileCreationTimeForDateAdded, out _))
        {
            return ValidationResult.Error("Provide --use-file-creation-time-for-date-added true|false when using the 'set' action.");
        }

        return ValidationResult.Success();
    }
}

public sealed class ServerConfigMetadataCommand : ApiCommand<ServerConfigMetadataSettings>
{
    public ServerConfigMetadataCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        ServerConfigMetadataSettings settings,
        Jellyfin.Cli.Api.Generated.JellyfinApiClient client,
        CancellationToken cancellationToken)
    {
        using var httpClient = CreateHttpClient();

        if (string.Equals(settings.Action, "set", StringComparison.OrdinalIgnoreCase))
        {
            var requestedValue = bool.Parse(settings.UseFileCreationTimeForDateAdded!);
            var payload = JsonSerializer.Serialize(new
            {
                UseFileCreationTimeForDateAdded = requestedValue,
            });

            using var response = await httpClient.PostAsync(
                "System/Configuration/metadata",
                new StringContent(payload, Encoding.UTF8, "application/json"),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                AnsiConsole.MarkupLine($"[red]Failed to update metadata config:[/] {(int)response.StatusCode} {Markup.Escape(response.ReasonPhrase ?? string.Empty)}");
                return 1;
            }
        }

        using var getResponse = await httpClient.GetAsync("System/Configuration/metadata", cancellationToken);
        if (!getResponse.IsSuccessStatusCode)
        {
            AnsiConsole.MarkupLine($"[red]Failed to load metadata config:[/] {(int)getResponse.StatusCode} {Markup.Escape(getResponse.ReasonPhrase ?? string.Empty)}");
            return 1;
        }

        await using var responseStream = await getResponse.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);
        var useFileCreationTime = document.RootElement.TryGetProperty("UseFileCreationTimeForDateAdded", out var valueElement) &&
                                  valueElement.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? valueElement.GetBoolean()
            : (bool?)null;

        if (settings.Json)
        {
            OutputHelper.WriteJson(new
            {
                useFileCreationTimeForDateAdded = useFileCreationTime,
                raw = document.RootElement,
            });
            return 0;
        }

        var table = OutputHelper.CreateTable("Field", "Value");
        table.AddRow("UseFileCreationTimeForDateAdded", useFileCreationTime?.ToString() ?? string.Empty);
        OutputHelper.WriteTable(table);

        if (string.Equals(settings.Action, "set", StringComparison.OrdinalIgnoreCase))
            AnsiConsole.MarkupLine("[dim]Metadata configuration updated.[/]");

        return 0;
    }
}

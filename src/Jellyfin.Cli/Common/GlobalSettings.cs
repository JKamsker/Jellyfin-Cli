using System.ComponentModel;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Common;

public class GlobalSettings : CommandSettings
{
    [CommandOption("--server <URL>")]
    [Description("Jellyfin server URL or hostname")]
    public string? Server { get; set; }

    [CommandOption("--profile <NAME>")]
    [Description("Profile name on the resolved host")]
    public string? Profile { get; set; }

    [CommandOption("--config <PATH>")]
    [Description("Path to config file (overrides default location)")]
    public string? ConfigPath { get; set; }

    [CommandOption("--token <TOKEN>")]
    [Description("Access token")]
    public string? Token { get; set; }

    [CommandOption("--api-key <KEY>")]
    [Description("API key")]
    public string? ApiKey { get; set; }

    [CommandOption("--user <ID>")]
    [Description("User context for user-scoped commands (id or 'me')")]
    public string? User { get; set; }

    [CommandOption("--json")]
    [Description("Emit JSON instead of table output")]
    public bool Json { get; set; }

    [CommandOption("--limit <N>")]
    [Description("Limit result count")]
    public int? Limit { get; set; }

    [CommandOption("--start <N>")]
    [Description("Start index for paged queries")]
    public int? Start { get; set; }

    [CommandOption("--all")]
    [Description("Auto-page until all results are fetched")]
    public bool All { get; set; }

    [CommandOption("--yes")]
    [Description("Skip confirmation prompts")]
    public bool Yes { get; set; }

    [CommandOption("--verbose")]
    [Description("Show request details")]
    public bool Verbose { get; set; }
}

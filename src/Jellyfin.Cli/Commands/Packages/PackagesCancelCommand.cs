using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Packages;

public sealed class PackagesCancelSettings : GlobalSettings
{
    [CommandArgument(0, "<ID>")]
    [Description("Installation ID")]
    public string Id { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        return Guid.TryParse(Id, out _)
            ? ValidationResult.Success()
            : ValidationResult.Error("A valid installation ID (GUID) is required.");
    }
}

public sealed class PackagesCancelCommand : ApiCommand<PackagesCancelSettings>
{
    public PackagesCancelCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, PackagesCancelSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        await client.Packages.Installing[Guid.Parse(settings.Id)].DeleteAsync(cancellationToken: cancellationToken);
        AnsiConsole.MarkupLine($"[green]Cancelled package installation [white]{settings.Id}[/].[/]");
        return 0;
    }
}

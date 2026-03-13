using System.ComponentModel;

using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Items;

public sealed class ItemsDownloadSettings : GlobalSettings
{
    [CommandArgument(0, "<ID>")]
    [Description("Item ID to download")]
    public string Id { get; set; } = string.Empty;

    [CommandOption("-o|--output <PATH>")]
    [Description("Output file path (defaults to item name in current directory)")]
    public string? Output { get; set; }

    public override ValidationResult Validate()
    {
        if (!Guid.TryParse(Id, out _))
            return ValidationResult.Error("A valid item ID (GUID) is required.");
        return ValidationResult.Success();
    }
}

public sealed class ItemsDownloadCommand : ApiCommand<ItemsDownloadSettings>
{
    public ItemsDownloadCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, ItemsDownloadSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var itemId = Guid.Parse(settings.Id);

        // Determine filename from the item if no output path given.
        var outputPath = settings.Output;
        if (string.IsNullOrEmpty(outputPath))
        {
            var item = await client.Items[itemId].GetAsync();
            var name = item?.Name ?? settings.Id;
            // Sanitize filename
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            outputPath = name;
        }

        AnsiConsole.MarkupLine($"Downloading to [blue]{Markup.Escape(outputPath)}[/]...");

        await using var stream = await client.Items[itemId].Download.GetAsync();
        if (stream is null)
        {
            AnsiConsole.MarkupLine("[red]Download returned no content. The item may not have a downloadable file.[/]");
            return 1;
        }

        await using var fileStream = File.Create(outputPath);
        await stream.CopyToAsync(fileStream);

        var fileInfo = new FileInfo(outputPath);
        var sizeDisplay = fileInfo.Length switch
        {
            >= 1_073_741_824 => $"{fileInfo.Length / 1_073_741_824.0:F2} GB",
            >= 1_048_576 => $"{fileInfo.Length / 1_048_576.0:F1} MB",
            >= 1_024 => $"{fileInfo.Length / 1_024.0:F0} KB",
            _ => $"{fileInfo.Length} bytes",
        };

        AnsiConsole.MarkupLine($"[green]Downloaded {sizeDisplay} to {Markup.Escape(outputPath)}.[/]");
        return 0;
    }
}

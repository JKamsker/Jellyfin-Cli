using Jellyfin.Cli.Api.Generated;
using Jellyfin.Cli.Api.Generated.Models;
using Jellyfin.Cli.Common;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Jellyfin.Cli.Commands.Server;

public sealed class ServerStorageCommand : ApiCommand<GlobalSettings>
{
    public ServerStorageCommand(ApiClientFactory clientFactory, CredentialStore credentialStore)
        : base(clientFactory, credentialStore)
    {
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, GlobalSettings settings, JellyfinApiClient client, CancellationToken cancellationToken)
    {
        var storage = await client.System.Info.Storage.GetAsync(cancellationToken: cancellationToken);
        if (storage is null)
        {
            AnsiConsole.MarkupLine("[yellow]No storage information returned.[/]");
            return 0;
        }

        if (settings.Json)
        {
            OutputHelper.WriteJson(storage);
            return 0;
        }

        var summary = OutputHelper.CreateTable("Area", "Path", "Free", "Total");
        AddFolder(summary, "Cache", storage.CacheFolder);
        AddFolder(summary, "Image Cache", storage.ImageCacheFolder);
        AddFolder(summary, "Internal Metadata", storage.InternalMetadataFolder);
        AddFolder(summary, "Logs", storage.LogFolder);
        AddFolder(summary, "Program Data", storage.ProgramDataFolder);
        AddFolder(summary, "Transcoding", storage.TranscodingTempFolder);
        AddFolder(summary, "Web", storage.WebFolder);
        OutputHelper.WriteTable(summary);

        if (storage.Libraries is { Count: > 0 })
        {
            var libraries = OutputHelper.CreateTable("Library", "Folder", "Device", "Storage Type", "Free", "Used", "Total");
            foreach (var library in storage.Libraries)
            {
                foreach (var folder in library.Folders ?? [])
                {
                    libraries.AddRow(
                        Markup.Escape(library.Name ?? string.Empty),
                        Markup.Escape(folder.Path ?? string.Empty),
                        Markup.Escape(folder.DeviceId ?? string.Empty),
                        Markup.Escape(folder.StorageType ?? string.Empty),
                        FormatBytes(folder.FreeSpace),
                        FormatBytes(folder.UsedSpace),
                        FormatBytes(ToTotalSpace(folder)));
                }
            }

            OutputHelper.WriteTable(libraries);
        }

        return 0;
    }

    private static void AddFolder(Table table, string area, FolderStorageDto? folder)
    {
        if (folder is null)
            return;

        table.AddRow(
            area,
            Markup.Escape(folder.Path ?? string.Empty),
            FormatBytes(folder.FreeSpace),
            FormatBytes(ToTotalSpace(folder)));
    }

    private static string FormatBytes(long? bytes)
    {
        if (bytes is null)
            return string.Empty;

        return bytes.Value switch
        {
            >= 1_073_741_824 => $"{bytes.Value / 1_073_741_824.0:F2} GB",
            >= 1_048_576 => $"{bytes.Value / 1_048_576.0:F2} MB",
            >= 1_024 => $"{bytes.Value / 1_024.0:F2} KB",
            _ => $"{bytes.Value} B",
        };
    }

    private static long? ToTotalSpace(FolderStorageDto? folder)
    {
        if (folder?.FreeSpace is null && folder?.UsedSpace is null)
            return null;

        return (folder?.FreeSpace ?? 0) + (folder?.UsedSpace ?? 0);
    }
}

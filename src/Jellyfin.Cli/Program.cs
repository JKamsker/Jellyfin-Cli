using Jellyfin.Cli.Commands.Auth;
using Jellyfin.Cli.Commands.Auth.Quick;
using Jellyfin.Cli.Commands.Backups;
using Jellyfin.Cli.Commands.Browse;
using Jellyfin.Cli.Commands.Collections;
using Jellyfin.Cli.Commands.Devices;
using Jellyfin.Cli.Commands.Items;
using Jellyfin.Cli.Commands.Items.Images;
using Jellyfin.Cli.Commands.Library;
using Jellyfin.Cli.Commands.LiveTv;
using Jellyfin.Cli.Commands.Packages;
using Jellyfin.Cli.Commands.Plugins;
using Jellyfin.Cli.Commands.Playlists;
using Jellyfin.Cli.Commands.Raw;
using Jellyfin.Cli.Commands.Server;
using Jellyfin.Cli.Commands.Sessions;
using Jellyfin.Cli.Commands.SyncPlay;
using Jellyfin.Cli.Commands.Tasks;
using Jellyfin.Cli.Commands.Users;
using Jellyfin.Cli.Common;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// Ensure Spectre.Console has enough width to render tables even when the
// terminal width cannot be detected (piped output, non-interactive shells).
if (Spectre.Console.AnsiConsole.Profile.Width < 160)
    Spectre.Console.AnsiConsole.Profile.Width = 160;

var services = new ServiceCollection();
services.AddSingleton<ApiClientFactory>();
services.AddSingleton<CredentialStore>();

var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);

app.Configure(config =>
{
    config.SetApplicationName("jf");
    config.SetApplicationVersion("0.1.0");

    config.AddBranch("auth", auth =>
    {
        auth.SetDescription("Sign in, inspect identity, and manage API keys");
        auth.AddCommand<LoginCommand>("login").WithDescription("Sign in with username and password");
        auth.AddCommand<WhoAmICommand>("whoami").WithDescription("Show the authenticated user and current server");
        auth.AddCommand<LogoutCommand>("logout").WithDescription("Remove stored credentials and end the current session");
        auth.AddCommand<AuthUsersCommand>("users").WithDescription("List public users shown on the login screen");
        auth.AddBranch("quick", quick =>
        {
            quick.SetDescription("Quick Connect flows");
            quick.AddCommand<QuickLoginCommand>("login").WithDescription("Start a Quick Connect login flow");
            quick.AddCommand<QuickApproveCommand>("approve").WithDescription("Approve a pending Quick Connect code");
            quick.AddCommand<QuickStatusCommand>("status").WithDescription("Check whether Quick Connect is enabled");
        });
        auth.AddBranch("keys", keys =>
        {
            keys.SetDescription("Manage API keys [[admin]]");
            keys.AddCommand<KeysListCommand>("list").WithDescription("List API keys");
            keys.AddCommand<KeysCreateCommand>("create").WithDescription("Create an API key");
            keys.AddCommand<KeysDeleteCommand>("delete").WithDescription("Revoke an API key");
        });
    });

    config.AddBranch("items", items =>
    {
        items.SetDescription("Browse, search, refresh, and update library items");
        items.AddCommand<ItemsListCommand>("list").WithDescription("Query items with filters");
        items.AddCommand<ItemsGetCommand>("get").WithDescription("Show one item by id");
        items.AddCommand<ItemsLatestCommand>("latest").WithDescription("Inspect Jellyfin's latest-items shelf");
        items.AddCommand<ItemsExplainLatestCommand>("explain-latest").WithDescription("Explain why an item is or is not visible in latest");
        items.AddCommand<ItemsSearchCommand>("search").WithDescription("Search names and titles");
        items.AddCommand<ItemsDatesCommand>("dates").WithDescription("Show date fields used for latest-items diagnostics");
        items.AddCommand<ItemsTreeCommand>("tree").WithDescription("Show a recursive item tree for diagnostics");
        items.AddCommand<ItemsResumeCommand>("resume").WithDescription("Show continue-watching items");
        items.AddCommand<ItemsFavoriteCommand>("favorite").WithDescription("Mark or unmark favorites");
        items.AddCommand<ItemsRefreshCommand>("refresh").WithDescription("Refresh metadata, images, or trickplay");
        items.AddCommand<ItemsUpdateCommand>("update").WithDescription("Update item metadata");
        items.AddCommand<ItemsDeleteCommand>("delete").WithDescription("Delete an item from the library and filesystem");
        items.AddCommand<ItemsRemoteSearchCommand>("remote-search").WithDescription("Search external metadata providers (TMDb, AniDB, IMDB, ...)");
        items.AddCommand<ItemsDownloadCommand>("download").WithDescription("Download original media or file");
        items.AddCommand<ItemsSimilarCommand>("similar").WithDescription("Find similar items");
        items.AddCommand<ItemsSuggestionsCommand>("suggestions").WithDescription("Show suggested items for the current user");
        items.AddCommand<ItemsCountsCommand>("counts").WithDescription("Show library item counts");
        items.AddCommand<ItemsPlaybackInfoCommand>("playback-info").WithDescription("Inspect playback media sources and streams");
        items.AddCommand<ItemsAncestorsCommand>("ancestors").WithDescription("Show parent hierarchy for an item");
        items.AddCommand<ItemsThemesCommand>("themes").WithDescription("List theme songs and videos for an item");
        items.AddBranch("images", images =>
        {
            images.SetDescription("Manage item artwork");
            images.AddCommand<ImagesListCommand>("list").WithDescription("List known images for an item");
            images.AddCommand<ImagesSetCommand>("set").WithDescription("Upload an image for an item");
            images.AddCommand<ImagesDeleteCommand>("delete").WithDescription("Delete an image");
        });
        items.AddBranch("remote-images", remoteImages =>
        {
            remoteImages.SetDescription("Browse or download remote item artwork");
            remoteImages.AddCommand<ItemsRemoteImagesListCommand>("list").WithDescription("List available remote images");
            remoteImages.AddCommand<ItemsRemoteImagesProvidersCommand>("providers").WithDescription("List remote image providers");
            remoteImages.AddCommand<ItemsRemoteImagesDownloadCommand>("download").WithDescription("Download a remote image");
        });
        items.AddBranch("subtitles", subtitles =>
        {
            subtitles.SetDescription("Search, download, upload, or delete subtitles");
            subtitles.AddCommand<ItemsSubtitlesSearchCommand>("search").WithDescription("Search remote subtitle providers");
            subtitles.AddCommand<ItemsSubtitlesDownloadCommand>("download").WithDescription("Download a remote subtitle");
            subtitles.AddCommand<ItemsSubtitlesUploadCommand>("upload").WithDescription("Upload an external subtitle file");
            subtitles.AddCommand<ItemsSubtitlesDeleteCommand>("delete").WithDescription("Delete an external subtitle file");
        });
        items.AddBranch("lyrics", lyrics =>
        {
            lyrics.SetDescription("Read, search, download, upload, or delete lyrics");
            lyrics.AddCommand<ItemsLyricsGetCommand>("get").WithDescription("Get lyrics for an audio item");
            lyrics.AddCommand<ItemsLyricsSearchCommand>("search").WithDescription("Search remote lyric providers");
            lyrics.AddCommand<ItemsLyricsDownloadCommand>("download").WithDescription("Download remote lyrics");
            lyrics.AddCommand<ItemsLyricsUploadCommand>("upload").WithDescription("Upload an external lyric file");
            lyrics.AddCommand<ItemsLyricsDeleteCommand>("delete").WithDescription("Delete external lyrics");
        });
        items.AddBranch("nfo", nfo =>
        {
            nfo.SetDescription("Inspect local NFO sidecar metadata");
            nfo.AddCommand<ItemsNfoInspectCommand>("inspect").WithDescription("Inspect derived local NFO metadata for an item");
        });
    });

    config.AddBranch("playlists", playlists =>
    {
        playlists.SetDescription("Create playlists and manage items or shares");
        playlists.AddCommand<PlaylistsCreateCommand>("create").WithDescription("Create a new playlist");
        playlists.AddCommand<PlaylistsGetCommand>("get").WithDescription("Show one playlist");
        playlists.AddCommand<PlaylistsUpdateCommand>("update").WithDescription("Update playlist metadata");
        playlists.AddBranch("items", pItems =>
        {
            pItems.SetDescription("Manage playlist items");
            pItems.AddCommand<PlaylistItemsListCommand>("list").WithDescription("List playlist items");
            pItems.AddCommand<PlaylistItemsAddCommand>("add").WithDescription("Add items to a playlist");
            pItems.AddCommand<PlaylistItemsRemoveCommand>("remove").WithDescription("Remove items from a playlist");
            pItems.AddCommand<PlaylistItemsMoveCommand>("move").WithDescription("Move an item within a playlist");
        });
    });

    config.AddBranch("collections", collections =>
    {
        collections.SetDescription("Create collections and add or remove items");
        collections.AddCommand<CollectionsCreateCommand>("create").WithDescription("Create a collection");
        collections.AddCommand<CollectionsAddCommand>("add").WithDescription("Add items to a collection");
        collections.AddCommand<CollectionsRemoveCommand>("remove").WithDescription("Remove items from a collection");
    });

    config.AddBranch("sessions", sessions =>
    {
        sessions.SetDescription("Inspect sessions and remote-control clients");
        sessions.AddCommand<SessionsListCommand>("list").WithDescription("List active sessions");
        sessions.AddCommand<SessionsPlayCommand>("play").WithDescription("Start playback on a session");
        sessions.AddCommand<SessionsStateCommand>("state").WithDescription("Send pause/resume/stop/seek commands");
        sessions.AddCommand<SessionsMessageCommand>("message").WithDescription("Display a message on a client");
        sessions.AddCommand<SessionsCommandSendCommand>("command").WithDescription("Send a general command payload");
    });

    config.AddBranch("users", users =>
    {
        users.SetDescription("Manage Jellyfin users [[admin]]");
        users.AddCommand<UsersListCommand>("list").WithDescription("List users");
        users.AddCommand<UsersGetCommand>("get").WithDescription("Show one user");
        users.AddCommand<UsersCreateCommand>("create").WithDescription("Create a user");
        users.AddCommand<UsersUpdateCommand>("update").WithDescription("Update a user");
        users.AddCommand<UsersDeleteCommand>("delete").WithDescription("Delete a user");
        users.AddCommand<UsersPasswordCommand>("password").WithDescription("Change a user's password");
        users.AddCommand<UsersPolicyCommand>("policy").WithDescription("Update a user's policy");
    });

    config.AddBranch("library", library =>
    {
        library.SetDescription("Scan libraries and manage virtual folders [[admin]]");
        library.AddCommand<LibraryScanCommand>("scan").WithDescription("Start a full library refresh");
        library.AddBranch("folders", folders =>
        {
            folders.SetDescription("List, create, rename, or remove virtual folders [[admin]]");
            folders.SetDefaultCommand<LibraryFoldersListCommand>();
            folders.AddCommand<LibraryFoldersListCommand>("list").WithDescription("List virtual folders");
            folders.AddCommand<LibraryFoldersAddCommand>("add").WithDescription("Create a virtual folder");
            folders.AddCommand<LibraryFoldersRenameCommand>("rename").WithDescription("Rename a virtual folder");
            folders.AddCommand<LibraryFoldersRemoveCommand>("remove").WithDescription("Remove a virtual folder");
        });
        library.AddBranch("paths", paths =>
        {
            paths.SetDescription("Manage media paths inside a virtual folder [[admin]]");
            paths.AddCommand<LibraryPathsAddCommand>("add").WithDescription("Add a media path to a library");
            paths.AddCommand<LibraryPathsUpdateCommand>("update").WithDescription("Update a media path entry");
            paths.AddCommand<LibraryPathsRemoveCommand>("remove").WithDescription("Remove a media path from a library");
        });
        library.AddCommand<LibraryOptionsCommand>("options").WithDescription("Show or update library options for one virtual folder");
        library.AddCommand<LibraryMediaCommand>("media").WithDescription("Show media folders");
    });

    config.AddBranch("genres", genres =>
    {
        genres.SetDescription("Browse library genres");
        genres.AddCommand<GenresListCommand>("list").WithDescription("List known genres");
    });

    config.AddBranch("studios", studios =>
    {
        studios.SetDescription("Browse library studios");
        studios.AddCommand<StudiosListCommand>("list").WithDescription("List known studios");
    });

    config.AddBranch("artists", artists =>
    {
        artists.SetDescription("Browse music artists or album artists");
        artists.AddCommand<ArtistsListCommand>("list").WithDescription("List artists or album artists");
    });

    config.AddBranch("persons", persons =>
    {
        persons.SetDescription("Browse actors, directors, and other people");
        persons.AddCommand<PersonsListCommand>("list").WithDescription("List people with optional person-type filters");
    });

    config.AddBranch("server", server =>
    {
        server.SetDescription("Health, logs, config, restart, and shutdown");
        server.AddCommand<ServerPingCommand>("ping").WithDescription("Ping the server");
        server.AddCommand<ServerInfoCommand>("info").WithDescription("Show public or authenticated server info");
        server.AddCommand<ServerStorageCommand>("storage").WithDescription("Show server storage information [[admin]]");
        server.AddCommand<ServerEndpointCommand>("endpoint").WithDescription("Show request endpoint information [[admin]]");
        server.AddCommand<ServerActivityCommand>("activity").WithDescription("Show activity log entries [[admin]]");
        server.AddCommand<ServerLogsCommand>("logs").WithDescription("List server logs [[admin]]");
        server.AddCommand<ServerBitrateTestCommand>("bitrate-test").WithDescription("Measure download throughput between the CLI and server");
        server.AddBranch("localization", localization =>
        {
            localization.SetDescription("List reference localization data");
            localization.AddCommand<ServerLocalizationCulturesCommand>("cultures").WithDescription("List supported cultures");
            localization.AddCommand<ServerLocalizationCountriesCommand>("countries").WithDescription("List known countries");
            localization.AddCommand<ServerLocalizationRatingsCommand>("ratings").WithDescription("List known parental ratings");
        });
        server.AddBranch("environment", environment =>
        {
            environment.SetDescription("Inspect the server filesystem [[admin]]");
            environment.AddCommand<ServerEnvironmentDrivesCommand>("drives").WithDescription("List available drives");
            environment.AddCommand<ServerEnvironmentLsCommand>("ls").WithDescription("List directory contents");
            environment.AddCommand<ServerEnvironmentValidateCommand>("validate").WithDescription("Validate that a path exists and is accessible");
        });
        server.AddBranch("log", log =>
        {
            log.SetDescription("Read individual server logs [[admin]]");
            log.AddCommand<ServerLogGetCommand>("get").WithDescription("Read one server log file");
        });
        server.AddBranch("config", serverConfig =>
        {
            serverConfig.SetDescription("Inspect and update server configuration [[admin]]");
            serverConfig.AddCommand<ServerConfigGetCommand>("get").WithDescription("Get full config or one named config section");
            serverConfig.AddCommand<ServerConfigSetCommand>("set").WithDescription("Update a named configuration section");
            serverConfig.AddCommand<ServerConfigBrandingCommand>("branding").WithDescription("View or update branding configuration");
            serverConfig.AddCommand<ServerConfigMetadataCommand>("metadata").WithDescription("Show or update metadata configuration");
        });
        server.AddCommand<ServerRestartCommand>("restart").WithDescription("Restart Jellyfin [[admin]]");
        server.AddCommand<ServerShutdownCommand>("shutdown").WithDescription("Shut down Jellyfin [[admin]]");
    });

    config.AddBranch("devices", devices =>
    {
        devices.SetDescription("Inspect or remove registered devices [[admin]]");
        devices.AddCommand<DevicesListCommand>("list").WithDescription("List registered devices");
        devices.AddCommand<DevicesGetCommand>("get").WithDescription("Show one device");
        devices.AddCommand<DevicesDeleteCommand>("delete").WithDescription("Remove a device");
        devices.AddBranch("options", deviceOptions =>
        {
            deviceOptions.SetDescription("Read or update per-device options [[admin]]");
            deviceOptions.AddCommand<DevicesOptionsGetCommand>("get").WithDescription("Get device options");
            deviceOptions.AddCommand<DevicesOptionsSetCommand>("set").WithDescription("Update device options");
        });
    });

    config.AddBranch("tasks", tasks =>
    {
        tasks.SetDescription("View and run scheduled tasks [[admin]]");
        tasks.AddCommand<TasksListCommand>("list").WithDescription("List tasks");
        tasks.AddCommand<TasksGetCommand>("get").WithDescription("Show one task");
        tasks.AddCommand<TasksStartCommand>("start").WithDescription("Start a task");
        tasks.AddCommand<TasksStopCommand>("stop").WithDescription("Stop a running task");
    });

    config.AddBranch("backups", backups =>
    {
        backups.SetDescription("Create and restore backups [[admin]]");
        backups.AddCommand<BackupsListCommand>("list").WithDescription("List backup archives");
        backups.AddCommand<BackupsInspectCommand>("inspect").WithDescription("Inspect a backup archive manifest");
        backups.AddCommand<BackupsCreateCommand>("create").WithDescription("Create a new backup");
        backups.AddCommand<BackupsRestoreCommand>("restore").WithDescription("Restore an archive and restart the server");
    });

    config.AddBranch("plugins", plugins =>
    {
        plugins.SetDescription("Configure, enable, disable, or uninstall plugins [[admin]]");
        plugins.AddCommand<PluginsListCommand>("list").WithDescription("List installed plugins");
        plugins.AddCommand<PluginsEnableCommand>("enable").WithDescription("Enable a plugin");
        plugins.AddCommand<PluginsDisableCommand>("disable").WithDescription("Disable a plugin");
        plugins.AddCommand<PluginsUninstallCommand>("uninstall").WithDescription("Uninstall a plugin");
        plugins.AddBranch("config", pluginConfig =>
        {
            pluginConfig.SetDescription("Read or update plugin configuration [[admin]]");
            pluginConfig.AddCommand<PluginsConfigGetCommand>("get").WithDescription("Read plugin configuration");
            pluginConfig.AddCommand<PluginsConfigSetCommand>("set").WithDescription("Update plugin configuration");
        });
    });

    config.AddBranch("packages", packages =>
    {
        packages.SetDescription("Browse repositories and install packages [[admin]]");
        packages.AddCommand<PackagesListCommand>("list").WithDescription("List packages available from configured repositories");
        packages.AddCommand<PackagesGetCommand>("get").WithDescription("Show details for one package");
        packages.AddCommand<PackagesInstallCommand>("install").WithDescription("Install a package");
        packages.AddCommand<PackagesCancelCommand>("cancel").WithDescription("Cancel an in-progress package installation");
        packages.AddBranch("repos", repos =>
        {
            repos.SetDescription("Inspect or replace configured package repositories [[admin]]");
            repos.AddCommand<PackagesReposListCommand>("list").WithDescription("List configured package repositories");
            repos.AddCommand<PackagesReposSetCommand>("set").WithDescription("Replace the configured package repository list");
        });
    });

    config.AddBranch("livetv", livetv =>
    {
        livetv.SetDescription("Channels, guide, recordings, timers, tuners, listings");
        livetv.AddCommand<LiveTvInfoCommand>("info").WithDescription("Show Live TV info");
        livetv.AddCommand<LiveTvChannelsCommand>("channels").WithDescription("List channels");
        livetv.AddCommand<LiveTvGuideCommand>("guide").WithDescription("Show program guide");
        livetv.AddCommand<LiveTvRecordingsCommand>("recordings").WithDescription("List recordings");
        livetv.AddCommand<LiveTvTimersCommand>("timers").WithDescription("List timers");
    });

    config.AddBranch("syncplay", syncplay =>
    {
        syncplay.SetDescription("Manage SyncPlay groups");
        syncplay.AddCommand<SyncPlayListCommand>("list").WithDescription("List SyncPlay groups");
        syncplay.AddCommand<SyncPlayCreateCommand>("create").WithDescription("Create a new group");
        syncplay.AddCommand<SyncPlayJoinCommand>("join").WithDescription("Join a group");
        syncplay.AddCommand<SyncPlayLeaveCommand>("leave").WithDescription("Leave current group");
    });

    config.AddBranch("raw", raw =>
    {
        raw.SetDescription("Low-level endpoint access");
        raw.AddCommand<RawGetCommand>("get").WithDescription("GET an endpoint");
        raw.AddCommand<RawPostCommand>("post").WithDescription("POST to an endpoint");
        raw.AddCommand<RawPutCommand>("put").WithDescription("PUT to an endpoint");
        raw.AddCommand<RawDeleteCommand>("delete").WithDescription("DELETE an endpoint");
    });
});

return await app.RunAsync(args);

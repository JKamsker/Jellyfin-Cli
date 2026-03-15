using Jellyfin.Cli.Commands.Auth;
using Jellyfin.Cli.Commands.Auth.Quick;
using Jellyfin.Cli.Commands.Backups;
using Jellyfin.Cli.Commands.Collections;
using Jellyfin.Cli.Commands.Devices;
using Jellyfin.Cli.Commands.Items;
using Jellyfin.Cli.Commands.Items.Images;
using Jellyfin.Cli.Commands.Library;
using Jellyfin.Cli.Commands.LiveTv;
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
        items.AddCommand<ItemsRemoteSearchCommand>("remote-search").WithDescription("Search external metadata providers (TMDb, AniDB, IMDB, ...)");
        items.AddCommand<ItemsDownloadCommand>("download").WithDescription("Download original media or file");
        items.AddBranch("images", images =>
        {
            images.SetDescription("Manage item artwork");
            images.AddCommand<ImagesListCommand>("list").WithDescription("List known images for an item");
            images.AddCommand<ImagesSetCommand>("set").WithDescription("Upload an image for an item");
            images.AddCommand<ImagesDeleteCommand>("delete").WithDescription("Delete an image");
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
        library.AddCommand<LibraryFoldersListCommand>("folders").WithDescription("List virtual folders");
        library.AddCommand<LibraryMediaCommand>("media").WithDescription("Show media folders");
    });

    config.AddBranch("server", server =>
    {
        server.SetDescription("Health, logs, config, restart, and shutdown");
        server.AddCommand<ServerPingCommand>("ping").WithDescription("Ping the server");
        server.AddCommand<ServerInfoCommand>("info").WithDescription("Show public or authenticated server info");
        server.AddCommand<ServerActivityCommand>("activity").WithDescription("Show activity log entries [[admin]]");
        server.AddCommand<ServerLogsCommand>("logs").WithDescription("List server logs [[admin]]");
        server.AddBranch("config", serverConfig =>
        {
            serverConfig.SetDescription("Inspect and update server configuration [[admin]]");
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

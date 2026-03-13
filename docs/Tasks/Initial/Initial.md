Based on the uploaded Jellyfin 10.11.6 OpenAPI, I’d make this a task-first CLI named `jf`, not a tag-for-tag endpoint mirror. The spec covers normal user/admin workflows, but it also exposes low-level transport and telemetry surfaces like raw audio/video/HLS routes, playback progress reporting, and SyncPlay group control, so the help system should foreground the common tasks and push the transport/debug pieces into an advanced area.

## Technology stack

- **Runtime**: .NET (dotnet) — cross-platform, single-file publish, good for CLI tooling
- **CLI framework**: [Spectre.Console.Cli](https://spectreconsole.net/cli/) — typed command/branch tree, rich help rendering, async support
- **API client**: [Kiota](https://learn.microsoft.com/en-us/openapi/kiota/) — generates a strongly-typed HTTP client directly from the Jellyfin OpenAPI spec; no hand-written request code

## Core UX rules

1. Top level is by **task domain**, not controller tag.
2. Leaves use a small verb set: `list`, `get`, `create`, `update`, `delete`, `add`, `remove`, `start`, `stop`, `enable`, `disable`.
3. Accept repeated arguments in the CLI, even when the API wants comma-delimited query values.
4. Show IDs prominently in every table, because most follow-up operations key off UUIDs.
5. Mark privileged commands as `[admin]` directly in help.
6. Keep `--json` on every command, and keep destructive commands interactive unless `--yes` is passed.
7. Hide transport/reporting/debug endpoints unless the user calls `raw` or asks for `--help-all`.

Those rules are mainly driven by the API shape: many list/query endpoints use `startIndex` and `limit`, many mutation endpoints accept comma-delimited `ids` or `entryIds`, most resource actions are UUID-based, and many operations are explicitly guarded by elevation or service-specific permissions.

## Recommended top-level tree

```text
jf
  auth
  items
  playlists
  collections
  sessions
  users
  library
  server
  devices
  tasks
  backups
  plugins
  livetv
  syncplay
  raw
```

That top-level shape matches the user-facing parts of the spec: auth/Quick Connect/current user/API keys; items plus images, subtitles, lyrics, favorites, ratings, playback, and metadata refresh; playlists and collections; session control; and user management.

The remaining branches are the clearly separate admin or advanced domains in the spec: library structure and scans; system/config/logs/restart; devices; scheduled tasks; backups; plugins; Live TV; SyncPlay; and low-level transport/debug access.

## Help-page contract

Every help page should use this shape:

```text
NAME
  jf <branch> - one-line purpose

USAGE
  jf <branch> <command> [options] [arguments]

DESCRIPTION
  1 short paragraph focused on tasks, not endpoints

COMMON TASKS
  3-5 example workflows

COMMANDS
  grouped by intent, not alphabetically

OPTIONS
  only the important ones for that branch

EXAMPLES
  realistic commands users will actually paste

SEE ALSO
  2-4 nearby branches
```

A good rule is that each help page should answer three things fast: “what is this for?”, “what do I type first?”, and “what are the dangerous commands?”

## `jf --help`

```text
NAME
  jf - Jellyfin command-line client

USAGE
  jf [global options] <command> [command options]

START HERE
  jf auth login
  jf auth quick login
  jf items list --limit 20
  jf sessions list
  jf server ping

COMMANDS
  auth         Sign in, inspect identity, and manage API keys
  items        Browse, search, refresh, and update library items
  playlists    Create playlists and manage items or shares
  collections  Create collections and add or remove items
  sessions     Inspect sessions and remote-control clients
  users        Manage Jellyfin users [admin]
  library      Scan libraries and manage virtual folders [admin]
  server       Health, logs, config, restart, and shutdown
  devices      Inspect or remove registered devices [admin]
  tasks        View and run scheduled tasks [admin]
  backups      Create and restore backups [admin]
  plugins      Configure, enable, disable, or uninstall plugins [admin]
  livetv       Channels, guide, recordings, timers, tuners, listings
  syncplay     Manage SyncPlay groups
  raw          Low-level endpoint access

GLOBAL OPTIONS
  --server <url>        Jellyfin base URL
  --token <token>       Access token
  --api-key <key>       API key
  --user <id|me>        User context for user-scoped commands
  --json                Emit JSON instead of table output
  --limit <n>           Limit result count
  --start <n>           Start index for paged queries
  --all                 Auto-page until all results are fetched
  --yes                 Skip confirmation prompts
  --verbose             Show request details
  --help-all            Show advanced and hidden commands
```

## `jf auth --help`

Auth should revolve around username/password login, Quick Connect, current-user inspection, public-login users, logout, and API-key management.

```text
NAME
  jf auth - Sign in and manage Jellyfin credentials

USAGE
  jf auth <command> [options]

COMMANDS
  login        Sign in with username and password
  quick        Quick Connect flows
  whoami       Show the authenticated user and current server
  logout       Remove stored credentials and end the current session
  users        List public users shown on the login screen
  keys         Manage API keys [admin]

COMMON TASKS
  jf auth login --server http://localhost --username alice
  jf auth quick login
  jf auth whoami
  jf auth users
  jf auth keys list

SEE ALSO
  jf users
  jf server
```

### `jf auth quick --help`

```text
NAME
  jf auth quick - Use Jellyfin Quick Connect

USAGE
  jf auth quick <command> [options]

COMMANDS
  login        Start a Quick Connect login flow for this CLI
  approve      Approve a pending Quick Connect code for a user
  status       Check whether Quick Connect is enabled

EXAMPLES
  jf auth quick login
  jf auth quick approve ABCD --user me
  jf auth quick status
```

I would keep provider-oriented auth endpoints hidden behind `--help-all`; they exist, but they are not the first-run UX. 

## `jf items --help`

The `items` branch should absorb query/list, item detail, resume, favorites, ratings, user data, refresh/update, images, subtitles, lyrics, playback info, remote metadata/image lookup, and download/file access.

```text
NAME
  jf items - Browse and manage library items

USAGE
  jf items <command> [options]

DISCOVERY
  list         Query items with filters
  get          Show one item by id
  search       Search names and titles
  resume       Show continue-watching items

USER ACTIONS
  favorite     Mark or unmark favorites
  rating       Set or clear personal ratings
  userdata     Get or update item-specific user data

METADATA
  refresh      Refresh metadata, images, or trickplay
  update       Update item metadata
  lookup       Search and apply remote metadata

ASSETS
  images       List, get, set, delete, reorder, and download remote images
  subtitles    Search, download, upload, or delete subtitles
  lyrics       Search, download, upload, or delete lyrics

PLAYBACK
  playback     Inspect playback info and media sources
  download     Download original media or file

COMMON TASKS
  jf items list --user me --limit 20
  jf items search "alien" --user me
  jf items get 8f9d...
  jf items resume --user me
  jf items favorite set 8f9d...
  jf items rating set 8f9d... --like
  jf items refresh 8f9d... --mode full --replace-images

COMMON OPTIONS
  --user <id|me>
  --search <text>
  --parent <item-id>
  --field <name>         Repeatable
  --sort <name>          Repeatable
  --desc
  --media-type <type>    Repeatable
  --limit <n>
  --start <n>
  --json

SEE ALSO
  jf playlists
  jf collections
  jf library
```

### `jf items images --help`

```text
NAME
  jf items images - Manage item artwork

USAGE
  jf items images <command> [options]

COMMANDS
  list         List known images for an item
  get          Download or print one image URL
  set          Upload an image for an item
  delete       Delete an image
  reorder      Change image index ordering
  remote list  Show remote image candidates
  remote get   Download a remote image onto the item

EXAMPLES
  jf items images list 8f9d...
  jf items images set 8f9d... primary cover.jpg
  jf items images delete 8f9d... backdrop --index 1
  jf items images reorder 8f9d... backdrop 3 0
  jf items images remote list 8f9d...
```

I would deliberately collapse the many type-specific remote-search endpoints into one `lookup` UX; the CLI can infer the appropriate backend from the item type or an explicit `--kind` flag.

## `jf playlists --help`

Playlists deserve a first-class branch because the API supports create/get/update, add/remove/move items, and per-user sharing/permissions. Collections are much smaller and can mirror the same style with just `create`, `add`, and `remove`.

```text
NAME
  jf playlists - Create playlists and manage contents or shares

USAGE
  jf playlists <command> [options]

COMMANDS
  create       Create a new playlist
  get          Show one playlist
  update       Update playlist metadata
  items        Add, remove, list, or move playlist items
  share        List, grant, update, or revoke user access

COMMON TASKS
  jf playlists create "Road Trip"
  jf playlists items add 41ab... 8f9d... 3c2e...
  jf playlists items remove 41ab... 2a91... 9d11...
  jf playlists items move 41ab... 2a91... 0
  jf playlists share grant 41ab... 6ee4...

SEE ALSO
  jf items
  jf collections
```

### `jf collections --help`

```text
NAME
  jf collections - Create collections and manage collection membership

USAGE
  jf collections <command> [options]

COMMANDS
  create       Create a collection
  add          Add items to a collection
  remove       Remove items from a collection

EXAMPLES
  jf collections create "Favorites 2026" 8f9d... 3c2e...
  jf collections add 51cd... 22aa... 44bb...
  jf collections remove 51cd... 44bb...
```

## `jf sessions --help`

The session branch should center on session discovery, playback control, message/command dispatch, and secondary-user management. The spec has session listing, play, playstate commands, general/system commands, message display, viewing, add/remove user, and session-end reporting.

```text
NAME
  jf sessions - Inspect sessions and remote-control clients

USAGE
  jf sessions <command> [options]

COMMANDS
  list         List active sessions
  play         Start playback on a session
  state        Send pause/resume/stop/seek/next/prev commands
  message      Display a message on a client
  command      Send a general command payload
  system       Send a system command
  view         Open an item or view on a client
  user         Add or remove a secondary user from a session

COMMON TASKS
  jf sessions list
  jf sessions list --active-within 300
  jf sessions play 91fa... 8f9d...
  jf sessions state pause 91fa...
  jf sessions state seek 91fa... --position 00:23:10
  jf sessions message 91fa... "Server maintenance in 5 minutes"
  jf sessions user add 91fa... 6ee4...

COMMON OPTIONS
  --device-id <id>
  --controllable-by <user-id|me>
  --active-within <seconds>
  --start-index <n>
  --start-position <hh:mm:ss|ticks>
  --json

SEE ALSO
  jf items
  jf syncplay
```

### `jf sessions state --help`

```text
NAME
  jf sessions state - Send playstate commands

USAGE
  jf sessions state <pause|resume|stop|next|prev|seek|rewind|ff|toggle> <session-id> [options]

OPTIONS
  --position <hh:mm:ss|ticks>   Required for seek

EXAMPLES
  jf sessions state pause 91fa...
  jf sessions state resume 91fa...
  jf sessions state seek 91fa... --position 01:10:00
```

## `jf library --help`

The library branch should be explicitly admin-oriented and separate from `items`. It should own scans, virtual folders, physical media paths, folder options, media-folder discovery, and path validation.

```text
NAME
  jf library - Scan libraries and manage virtual folders [admin]

USAGE
  jf library <command> [options]

COMMANDS
  scan         Start a full library refresh
  folders      List, add, rename, or remove virtual folders
  paths        List, add, update, or remove physical media paths
  media        Show media folders or physical paths
  options      Get or update library options
  validate     Validate a filesystem path before using it

COMMON TASKS
  jf library scan
  jf library folders list
  jf library folders add "Movies" --type movies --path /srv/media/movies
  jf library paths add "Movies" /srv/media/more-movies
  jf library validate /srv/media/movies

SEE ALSO
  jf items
  jf server
```

## `jf users --help`

Users should be a small, admin-first management branch. Keep identity inspection under `auth whoami`; keep admin CRUD, password, and policy changes under `users`.

```text
NAME
  jf users - Manage Jellyfin users [admin]

USAGE
  jf users <command> [options]

COMMANDS
  list         List users
  get          Show one user
  create       Create a user
  update       Update a user
  delete       Delete a user
  password     Change a user's password
  policy       Update a user's policy
  me           Show the current authenticated user

COMMON TASKS
  jf users list
  jf users get 6ee4...
  jf users create alice
  jf users password set 6ee4...
  jf users delete 6ee4... --yes

SEE ALSO
  jf auth
  jf sessions
```

## `jf server --help`

Server should collect the system/config/log view of Jellyfin: ping, info, endpoint, storage, activity, logs, config, restart, and shutdown.

```text
NAME
  jf server - Health, logs, config, and lifecycle

USAGE
  jf server <command> [options]

COMMANDS
  ping         Ping the server
  info         Show public or authenticated server info
  endpoint     Show request-endpoint information
  storage      Show storage details [admin]
  activity     Show activity log entries [admin]
  logs         List or fetch server logs [admin]
  config       Get or update server configuration [admin]
  restart      Restart Jellyfin [admin]
  shutdown     Shut down Jellyfin [admin]

COMMON TASKS
  jf server ping
  jf server info
  jf server logs list
  jf server logs get --name jellyfin.log
  jf server config get
  jf server restart --yes

SEE ALSO
  jf library
  jf tasks
  jf backups
```

## `jf tasks --help`

Scheduled tasks are a clean standalone admin branch: list, inspect, start, stop, and edit triggers.

```text
NAME
  jf tasks - View and run scheduled tasks [admin]

USAGE
  jf tasks <command> [options]

COMMANDS
  list         List tasks
  get          Show one task
  start        Start a task
  stop         Stop a running task
  triggers     Update task triggers

EXAMPLES
  jf tasks list
  jf tasks get 0d4d...
  jf tasks start 0d4d...
  jf tasks stop 0d4d...
```

## `jf backups --help`

Backups should be explicit and careful. The spec supports list, create, inspect manifest, and restore, and restore restarts the server to apply the backup.

```text
NAME
  jf backups - Create and restore backups [admin]

USAGE
  jf backups <command> [options]

COMMANDS
  list         List backup archives
  create       Create a new backup
  inspect      Read the manifest from an archive
  restore      Restore an archive and restart the server

OPTIONS FOR CREATE
  --metadata
  --trickplay
  --subtitles
  --database

COMMON TASKS
  jf backups list
  jf backups create --metadata --database --subtitles
  jf backups inspect /backup/jellyfin-2026-03-13.zip
  jf backups restore jellyfin-2026-03-13.zip --yes
```

## Smaller first-class branches

I’d keep these as separate top-level branches, but their help pages can be shorter.

The spec has a complete plugins area with installed-plugin listing, configuration get/set, enable/disable, and uninstall; a devices area with list/get/options/delete; a Live TV area with channels, guide, recordings, timers, series timers, tuners, and listing providers; a SyncPlay area with group lifecycle plus queue/playback control; and collection management with create/add/remove.

```text
jf plugins --help
  list, config get|set, enable, disable, uninstall

jf devices --help
  list, get, options get|set, delete

jf livetv --help
  info, channels, guide, recordings, timers, series-timers, tuners, listings

jf syncplay --help
  list, get, create, join, leave, pause, resume, seek, queue, next, prev, stop
```

## `jf raw --help`

I would explicitly hide low-level streaming/HLS/telemetry and similar endpoint-shaped operations behind `raw` or `--help-all`, instead of cluttering normal help with them.

```text
NAME
  jf raw - Call a Jellyfin endpoint directly

USAGE
  jf raw <get|post|put|delete> <path> [options]

OPTIONS
  --query <k=v>          Repeatable
  --header <k:v>         Repeatable
  --body <json|@file>
  --accept <mime>
  --download <file>
  --json
  --yes

EXAMPLES
  jf raw get /System/Info
  jf raw post /Library/Refresh
  jf raw get /Items/8f9d.../PlaybackInfo
  jf raw get /Videos/8f9d.../master.m3u8 --accept application/x-mpegURL
```

## The important UX choices

The most important product decisions here are:

* `items` and `library` are separate.
* `sessions` is first-class, because remote control is a real Jellyfin use case.
* `auth` owns identity and API keys, not `users`.
* playlist sharing lives under `playlists share`, not under `users`.
* low-level stream/HLS/reporting routes are hidden by default.
* the CLI accepts repeated arguments, even when the HTTP API wants comma-delimited lists.

That gives you a CLI whose help reads like a product manual instead of a controller dump.

Next implementation step is mapping each top-level help page to a Spectre.Console.Cli `AddBranch()` tree, with `raw` and `--help-all` hidden by default. The Kiota-generated client lives in a separate project/assembly and is injected into each command via the DI container that Spectre.Console.Cli exposes through `ITypeRegistrar`.

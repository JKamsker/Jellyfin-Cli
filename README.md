# Jellyfin CLI (`jf`)

[![NuGet](https://img.shields.io/nuget/v/JellyfinCli.svg)](https://www.nuget.org/packages/JellyfinCli)
[![NuGet Downloads](https://img.shields.io/nuget/dt/JellyfinCli.svg)](https://www.nuget.org/packages/JellyfinCli)
[![CI](https://github.com/JKamsker/Jellyfin-Cli/actions/workflows/ci.yml/badge.svg)](https://github.com/JKamsker/Jellyfin-Cli/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/JKamsker/Jellyfin-Cli/blob/main/LICENSE)

A command-line interface for [Jellyfin](https://jellyfin.org/) media server, built with .NET 10, Spectre.Console, and Kiota.

## Installation

```bash
dotnet tool install -g JellyfinCli
```

## Quick Start

```bash
# Login with username/password
jf auth login --server https://your-server.example.com --username admin

# Login with API key
jf auth login --server https://your-server.example.com --api-key YOUR_KEY

# Check connection
jf server ping
jf server info

# Browse your library
jf items list
jf items search --query "Breaking Bad"
```

## Commands

| Group | Commands |
|-------|----------|
| `auth` | `login`, `logout`, `status` |
| `server` | `info`, `ping`, `logs`, `activity`, `restart`, `shutdown` |
| `items` | `list`, `get`, `search`, `latest`, `favorite`, `unfavorite`, `delete`, `resume` |
| `users` | `list`, `get`, `create`, `delete`, `password` |
| `sessions` | `list`, `state` |
| `library` | `refresh`, `scan` |
| `playlists` | `list`, `get`, `create`, `delete`, `add`, `remove` |
| `collections` | `list`, `create`, `delete`, `add`, `remove` |
| `devices` | `list`, `get`, `delete` |
| `tasks` | `list`, `get`, `run` |
| `plugins` | `list`, `enable`, `disable`, `uninstall` |
| `backups` | `list`, `create`, `restore` |
| `livetv` | `channels`, `recordings`, `timers` |
| `syncplay` | `list`, `new`, `join`, `leave` |
| `raw` | `get`, `post`, `put`, `delete` |

## Global Options

| Option | Description |
|--------|-------------|
| `--server` | Server URL (saved after login) |
| `--token` | Auth token (saved after login) |
| `--api-key` | API key for authentication |
| `--user` | User ID (use `me` for current user) |
| `--json` | Output as JSON |
| `--verbose` | Verbose output |
| `--limit` | Limit number of results |
| `--start` | Pagination offset |

## Frequent Workflows

### List libraries and browse their content

```bash
# List all media libraries (shows Id, Name, and Type for each library)
jf library media

# List items inside a specific library using its Id
jf items list --parent <LIBRARY_ID>

# Filter by item type (e.g. Series, Movie, Episode)
jf items list --parent <LIBRARY_ID> --type Series

# Sort and limit results
jf items list --parent <LIBRARY_ID> --type Series --sort SortName --limit 10

# Search recursively within a library
jf items list --parent <LIBRARY_ID> --recursive --search "dragon"

# Output as JSON for scripting
jf items list --parent <LIBRARY_ID> --type Movie --json
```

### More use cases

Detailed guides with examples for common workflows:

- [Authentication](docs/Common_UseCases/01_Authentication.md) -- Login, logout, Quick Connect, API key management
- [Browsing Libraries](docs/Common_UseCases/02_Browsing_Libraries.md) -- List libraries, filter, sort, search, view item details
- [Server Administration](docs/Common_UseCases/03_Server_Administration.md) -- Health checks, user management, tasks, plugins, backups
- [Playback Control](docs/Common_UseCases/04_Playback_Control.md) -- Sessions, remote play/pause/seek, client messages
- [Playlists and Collections](docs/Common_UseCases/05_Playlists_And_Collections.md) -- Create, manage items, reorder
- [Media Management](docs/Common_UseCases/06_Media_Management.md) -- Update metadata, refresh, images, favorites, downloads
- [Scripting and Automation](docs/Common_UseCases/07_Scripting_And_Automation.md) -- JSON output, pagination, raw API, shell integration
- [SyncPlay](docs/Common_UseCases/08_SyncPlay.md) -- Synchronized group watching

## Building from Source

```bash
git clone https://github.com/JKamsker/Jellyfin-Cli.git
cd Jellyfin-Cli
dotnet build
dotnet run --project src/Jellyfin.Cli -- server ping --server https://your-server.example.com
```

## License

MIT

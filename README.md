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

## Building from Source

```bash
git clone https://github.com/JKamsker/Jellyfin-Cli.git
cd Jellyfin-Cli
dotnet build
dotnet run --project src/Jellyfin.Cli -- server ping --server https://your-server.example.com
```

## License

MIT

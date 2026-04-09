# `raw`

[README](../README.md) › `raw`

Low-level endpoint access

## Subcommands

- [delete](#command-raw-delete) — DELETE an endpoint
- [get](#command-raw-get) — GET an endpoint
- [post](#command-raw-post) — POST to an endpoint
- [put](#command-raw-put) — PUT to an endpoint

## Examples

- `raw get /System/Info`

<a id="command-raw-delete"></a>
## `raw delete`

DELETE an endpoint

### Arguments

| Name | Required | Arity | Accepted Values | Group | Description |
| --- | --- | --- | --- | --- | --- |
| PATH | Yes | 1 | — | — | API path (e.g. /System/Info) |

### Options

| Name | Aliases | Value | Required | Recursive | Scope | Group | Description | Arguments |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| --accept | — | <ACCEPT> | No | No | Declared | — | Accept header value | ACCEPT · required · arity 1 |
| --all | — | flag | No | No | Declared | — | Auto-page until all results are fetched | — |
| --api-key | — | <KEY> | No | No | Declared | — | API key | KEY · required · arity 1 |
| --body | — | <BODY> | No | No | Declared | — | Request body (JSON string) | BODY · required · arity 1 |
| --config | — | <PATH> | No | No | Declared | — | Path to config file (overrides default location) | PATH · required · arity 1 |
| --download | — | <FILE> | No | No | Declared | — | Download response to a file instead of printing | FILE · required · arity 1 |
| --header | — | <HEADER> | No | No | Declared | — | Extra headers (Key: Value), can be repeated | HEADER · required · arity 1 |
| --json | — | flag | No | No | Declared | — | Emit JSON instead of table output | — |
| --limit | — | <N> | No | No | Declared | — | Limit result count | N · required · arity 1 |
| --profile | — | <NAME> | No | No | Declared | — | Profile name on the resolved host | NAME · required · arity 1 |
| --query | — | <QUERY> | No | No | Declared | — | Query string parameters (key=value), can be repeated | QUERY · required · arity 1 |
| --server | — | <URL> | No | No | Declared | — | Jellyfin server URL or hostname | URL · required · arity 1 |
| --start | — | <N> | No | No | Declared | — | Start index for paged queries | N · required · arity 1 |
| --token | — | <TOKEN> | No | No | Declared | — | Access token | TOKEN · required · arity 1 |
| --user | — | <ID> | No | No | Declared | — | User context for user-scoped commands (id or 'me') | ID · required · arity 1 |
| --verbose | — | flag | No | No | Declared | — | Show request details | — |
| --yes | — | flag | No | No | Declared | — | Skip confirmation prompts | — |

### Examples

- `raw delete /Items/12345`

<a id="command-raw-get"></a>
## `raw get`

GET an endpoint

### Arguments

| Name | Required | Arity | Accepted Values | Group | Description |
| --- | --- | --- | --- | --- | --- |
| PATH | Yes | 1 | — | — | API path (e.g. /System/Info) |

### Options

| Name | Aliases | Value | Required | Recursive | Scope | Group | Description | Arguments |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| --accept | — | <ACCEPT> | No | No | Declared | — | Accept header value | ACCEPT · required · arity 1 |
| --all | — | flag | No | No | Declared | — | Auto-page until all results are fetched | — |
| --api-key | — | <KEY> | No | No | Declared | — | API key | KEY · required · arity 1 |
| --body | — | <BODY> | No | No | Declared | — | Request body (JSON string) | BODY · required · arity 1 |
| --config | — | <PATH> | No | No | Declared | — | Path to config file (overrides default location) | PATH · required · arity 1 |
| --download | — | <FILE> | No | No | Declared | — | Download response to a file instead of printing | FILE · required · arity 1 |
| --header | — | <HEADER> | No | No | Declared | — | Extra headers (Key: Value), can be repeated | HEADER · required · arity 1 |
| --json | — | flag | No | No | Declared | — | Emit JSON instead of table output | — |
| --limit | — | <N> | No | No | Declared | — | Limit result count | N · required · arity 1 |
| --profile | — | <NAME> | No | No | Declared | — | Profile name on the resolved host | NAME · required · arity 1 |
| --query | — | <QUERY> | No | No | Declared | — | Query string parameters (key=value), can be repeated | QUERY · required · arity 1 |
| --server | — | <URL> | No | No | Declared | — | Jellyfin server URL or hostname | URL · required · arity 1 |
| --start | — | <N> | No | No | Declared | — | Start index for paged queries | N · required · arity 1 |
| --token | — | <TOKEN> | No | No | Declared | — | Access token | TOKEN · required · arity 1 |
| --user | — | <ID> | No | No | Declared | — | User context for user-scoped commands (id or 'me') | ID · required · arity 1 |
| --verbose | — | flag | No | No | Declared | — | Show request details | — |
| --yes | — | flag | No | No | Declared | — | Skip confirmation prompts | — |

### Examples

- `raw get /System/Info`

<a id="command-raw-post"></a>
## `raw post`

POST to an endpoint

### Arguments

| Name | Required | Arity | Accepted Values | Group | Description |
| --- | --- | --- | --- | --- | --- |
| PATH | Yes | 1 | — | — | API path (e.g. /System/Info) |

### Options

| Name | Aliases | Value | Required | Recursive | Scope | Group | Description | Arguments |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| --accept | — | <ACCEPT> | No | No | Declared | — | Accept header value | ACCEPT · required · arity 1 |
| --all | — | flag | No | No | Declared | — | Auto-page until all results are fetched | — |
| --api-key | — | <KEY> | No | No | Declared | — | API key | KEY · required · arity 1 |
| --body | — | <BODY> | No | No | Declared | — | Request body (JSON string) | BODY · required · arity 1 |
| --config | — | <PATH> | No | No | Declared | — | Path to config file (overrides default location) | PATH · required · arity 1 |
| --download | — | <FILE> | No | No | Declared | — | Download response to a file instead of printing | FILE · required · arity 1 |
| --header | — | <HEADER> | No | No | Declared | — | Extra headers (Key: Value), can be repeated | HEADER · required · arity 1 |
| --json | — | flag | No | No | Declared | — | Emit JSON instead of table output | — |
| --limit | — | <N> | No | No | Declared | — | Limit result count | N · required · arity 1 |
| --profile | — | <NAME> | No | No | Declared | — | Profile name on the resolved host | NAME · required · arity 1 |
| --query | — | <QUERY> | No | No | Declared | — | Query string parameters (key=value), can be repeated | QUERY · required · arity 1 |
| --server | — | <URL> | No | No | Declared | — | Jellyfin server URL or hostname | URL · required · arity 1 |
| --start | — | <N> | No | No | Declared | — | Start index for paged queries | N · required · arity 1 |
| --token | — | <TOKEN> | No | No | Declared | — | Access token | TOKEN · required · arity 1 |
| --user | — | <ID> | No | No | Declared | — | User context for user-scoped commands (id or 'me') | ID · required · arity 1 |
| --verbose | — | flag | No | No | Declared | — | Show request details | — |
| --yes | — | flag | No | No | Declared | — | Skip confirmation prompts | — |

### Examples

- `raw post /Sessions/Capabilities --body {}`

<a id="command-raw-put"></a>
## `raw put`

PUT to an endpoint

### Arguments

| Name | Required | Arity | Accepted Values | Group | Description |
| --- | --- | --- | --- | --- | --- |
| PATH | Yes | 1 | — | — | API path (e.g. /System/Info) |

### Options

| Name | Aliases | Value | Required | Recursive | Scope | Group | Description | Arguments |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| --accept | — | <ACCEPT> | No | No | Declared | — | Accept header value | ACCEPT · required · arity 1 |
| --all | — | flag | No | No | Declared | — | Auto-page until all results are fetched | — |
| --api-key | — | <KEY> | No | No | Declared | — | API key | KEY · required · arity 1 |
| --body | — | <BODY> | No | No | Declared | — | Request body (JSON string) | BODY · required · arity 1 |
| --config | — | <PATH> | No | No | Declared | — | Path to config file (overrides default location) | PATH · required · arity 1 |
| --download | — | <FILE> | No | No | Declared | — | Download response to a file instead of printing | FILE · required · arity 1 |
| --header | — | <HEADER> | No | No | Declared | — | Extra headers (Key: Value), can be repeated | HEADER · required · arity 1 |
| --json | — | flag | No | No | Declared | — | Emit JSON instead of table output | — |
| --limit | — | <N> | No | No | Declared | — | Limit result count | N · required · arity 1 |
| --profile | — | <NAME> | No | No | Declared | — | Profile name on the resolved host | NAME · required · arity 1 |
| --query | — | <QUERY> | No | No | Declared | — | Query string parameters (key=value), can be repeated | QUERY · required · arity 1 |
| --server | — | <URL> | No | No | Declared | — | Jellyfin server URL or hostname | URL · required · arity 1 |
| --start | — | <N> | No | No | Declared | — | Start index for paged queries | N · required · arity 1 |
| --token | — | <TOKEN> | No | No | Declared | — | Access token | TOKEN · required · arity 1 |
| --user | — | <ID> | No | No | Declared | — | User context for user-scoped commands (id or 'me') | ID · required · arity 1 |
| --verbose | — | flag | No | No | Declared | — | Show request details | — |
| --yes | — | flag | No | No | Declared | — | Skip confirmation prompts | — |

### Examples

- `raw put /Sessions/Capabilities --body {}`

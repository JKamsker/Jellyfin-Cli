# Server Administration

## Health check

```bash
# Ping the server and measure response time
jf server ping

# Show server version, architecture, and system info
jf server info

# Show endpoint and storage information
jf server endpoint
jf server storage
```

## Configuration and environment

```bash
# Dump the full server configuration
jf server config get

# Read a named configuration section
jf server config get MetadataOptions

# View branding settings such as disclaimer text and custom CSS
jf server config branding

# Inspect reference localization data
jf server localization cultures
jf server localization countries
jf server localization ratings

# Inspect the server filesystem
jf server environment drives
jf server environment ls /srv/media
jf server environment validate /srv/media
```

## Library management

```bash
# Trigger a full library scan
jf library scan

# Refresh metadata for a specific item
jf items refresh <ITEM_ID>

# Full metadata refresh with image and metadata replacement
jf items refresh <ITEM_ID> --mode full --replace-images --replace-metadata
```

## Scheduled tasks

```bash
# List all scheduled tasks
jf tasks list

# View details of a specific task
jf tasks get <TASK_ID>

# Start a task manually
jf tasks start <TASK_ID>

# Stop a running task
jf tasks stop <TASK_ID>

# Inspect or replace task triggers
jf tasks triggers <TASK_ID>
jf tasks triggers set <TASK_ID> --daily 03:00
```

## User management

```bash
# List all users
jf users list

# View a specific user
jf users get <USER_ID>

# Create a new user
jf users create "newuser" --password "pass123"

# Change a user's password
jf users password <USER_ID>

# Update user policy (e.g. admin privileges)
jf users policy <USER_ID>

# Delete a user (prompts for confirmation)
jf users delete <USER_ID>
```

## Activity log

```bash
# Show recent activity
jf server activity

# Show the last 5 entries as JSON
jf server activity --json --limit 5
```

## Server logs

```bash
# List available log files
jf server logs

# Read one log file
jf server log get jellyfin.log

# As JSON
jf server logs --json
```

## Plugins

```bash
# List installed plugins
jf plugins list

# Read or update plugin configuration
jf plugins config get <PLUGIN_ID>
jf plugins config set <PLUGIN_ID> --file plugin-config.json

# Disable a plugin
jf plugins disable --plugin-id <PLUGIN_ID> --version <VERSION>

# Enable a plugin
jf plugins enable --plugin-id <PLUGIN_ID> --version <VERSION>

# Uninstall a plugin (prompts for confirmation)
jf plugins uninstall --plugin-id <PLUGIN_ID> --version <VERSION>
```

## Devices

```bash
# List all registered devices
jf devices list

# View device details
jf devices get --id <DEVICE_ID>

# Remove a device (prompts for confirmation)
jf devices delete --id <DEVICE_ID>

# View or update the device display name
jf devices options get <DEVICE_ID>
jf devices options set <DEVICE_ID> --name "Living Room TV"
```

## Packages

```bash
# Browse packages from configured repositories
jf packages list
jf packages get Jellyfin.Plugin.SubtitleExtract

# Install a specific package version
jf packages install Jellyfin.Plugin.SubtitleExtract --version 1.2.3.0

# Show or replace package repositories
jf packages repos list
jf packages repos set --file repositories.json
```

## Network diagnostics

```bash
# Estimate throughput between the CLI and the server
jf server bitrate-test
jf server bitrate-test --size 1048576
```

## Server restart and shutdown (admin)

```bash
# Restart (prompts for confirmation)
jf server restart

# Shutdown (prompts for confirmation)
jf server shutdown

# Skip confirmation prompts
jf server restart --yes
```

## Backups (admin)

```bash
# List existing backups
jf backups list

# Inspect the manifest stored in an archive
jf backups inspect /path/to/backup.zip

# Create a backup
jf backups create --metadata --database

# Restore a backup (restarts server)
jf backups restore <ARCHIVE_FILE>
```

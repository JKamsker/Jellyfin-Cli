# Server Administration

## Health check

```bash
# Ping the server and measure response time
jf server ping

# Show server version, architecture, and system info
jf server info
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

# As JSON
jf server logs --json
```

## Plugins

```bash
# List installed plugins
jf plugins list

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

# Create a backup
jf backups create --metadata --database

# Restore a backup (restarts server)
jf backups restore <ARCHIVE_FILE>
```

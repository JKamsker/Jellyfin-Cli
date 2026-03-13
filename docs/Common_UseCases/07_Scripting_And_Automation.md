# Scripting and Automation

## JSON output

All list commands support `--json` for machine-readable output:

```bash
jf items list --parent <LIBRARY_ID> --type Movie --json
jf users list --json
jf sessions list --json
jf plugins list --json
```

## Skip confirmation prompts

Use `--yes` to skip interactive prompts in scripts:

```bash
jf server restart --yes
jf users delete <USER_ID> --yes
jf collections remove <COLLECTION_ID> --items <ITEM_ID> --yes
```

## Pagination

Fetch all results with `--all`, or paginate manually:

```bash
# Fetch everything
jf items list --parent <LIBRARY_ID> --type Movie --all --json

# Manual pagination
jf items list --parent <LIBRARY_ID> --limit 50 --start 0 --json
jf items list --parent <LIBRARY_ID> --limit 50 --start 50 --json
```

## Raw API access

For endpoints not covered by built-in commands, use `raw`:

```bash
# GET request
jf raw get System/Info/Public

# POST with a body
jf raw post ScheduledTasks/Running/<TASK_ID> --body '{}'

# Custom headers and query parameters
jf raw get Items --query "parentId=<ID>" --query "includeItemTypes=Movie"

# Download a file from the API
jf raw get Items/<ITEM_ID>/Download --download output.mkv
```

## Combining with shell tools

```bash
# Get all movie IDs as a plain list
jf items list --parent <LIBRARY_ID> --type Movie --all --json \
  | jq -r '.items[].id'

# Count items in a library
jf items list --parent <LIBRARY_ID> --type Series --json \
  | jq '.totalRecordCount'

# Find items without a production year
jf items list --parent <LIBRARY_ID> --type Movie --all --json \
  | jq '[.items[] | select(.year == null)] | length'
```

## Connecting to a different server

Override credentials per-command without affecting stored login:

```bash
jf items list --server https://other-server.example.com --api-key OTHER_KEY
```

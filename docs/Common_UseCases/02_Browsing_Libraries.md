# Browsing Libraries

## List all libraries

```bash
jf library media
```

Use `--json` for scripting-friendly output:

```bash
jf library media --json
```

Example JSON output:

```json
[
  {
    "name": "Anime Shows",
    "id": "29391378-c411-8b35-b77f-84980d25f0a6",
    "collectionType": "Tvshows",
    "type": "CollectionFolder"
  },
  {
    "name": "Movies",
    "id": "7a2175bc-cb1f-1a94-152c-bd2b2bae8f6d",
    "collectionType": "Movies",
    "type": "CollectionFolder"
  }
]
```

## List items in a library

Use the library ID from `jf library media` as the `--parent` value:

```bash
jf items list --parent <LIBRARY_ID>
```

## Filter by item type

```bash
# Only series
jf items list --parent <LIBRARY_ID> --type Series

# Only movies
jf items list --parent <LIBRARY_ID> --type Movie

# Only episodes
jf items list --parent <LIBRARY_ID> --type Episode --recursive
```

## Sort and limit results

```bash
# Sort alphabetically
jf items list --parent <LIBRARY_ID> --type Series --sort SortName

# Recently added first
jf items list --parent <LIBRARY_ID> --type Series --sort DateCreated --desc

# Show only the top 10
jf items list --parent <LIBRARY_ID> --type Movie --sort SortName --limit 10

# Paginate (skip first 10, show next 10)
jf items list --parent <LIBRARY_ID> --limit 10 --start 10
```

Available sort fields: `SortName`, `DateCreated`, `PremiereDate`, `ProductionYear`, `Random`.

## Search within a library

```bash
# Search by name within a library
jf items list --parent <LIBRARY_ID> --search "dragon"

# Recursive search (includes episodes, seasons, etc.)
jf items list --parent <LIBRARY_ID> --recursive --search "dragon"
```

## View item details

```bash
jf items get <ITEM_ID>

# JSON output with full metadata
jf items get <ITEM_ID> --json
```

## Global search across all libraries

```bash
jf items search "Breaking Bad"

# Filter search by type
jf items search "Breaking Bad" --type Series
```

## List virtual folders (admin)

Shows the underlying filesystem paths for each library:

```bash
jf library folders
```

## Request extra fields

```bash
# Include overview and file path in results
jf items list --parent <LIBRARY_ID> --field Overview,Path --json
```

# Playlists and Collections

## Playlists

### Create a playlist

```bash
# Create an empty playlist
jf playlists create --name "My Playlist"

# Create with initial items
jf playlists create --name "My Playlist" --items <ITEM_ID_1>,<ITEM_ID_2>

# Create a public playlist
jf playlists create --name "Shared Playlist" --public
```

### View a playlist

```bash
jf playlists get <PLAYLIST_ID>
```

### Manage playlist items

```bash
# List items in a playlist
jf playlists items list <PLAYLIST_ID>

# Add items
jf playlists items add <PLAYLIST_ID> --items <ITEM_ID_1>,<ITEM_ID_2>

# Remove items (uses playlist entry IDs, not item IDs)
jf playlists items remove <PLAYLIST_ID> --entry-ids <ENTRY_ID_1>,<ENTRY_ID_2>

# Reorder an item within the playlist
jf playlists items move <PLAYLIST_ID> --item-id <ITEM_ID> --new-index 0
```

### Update playlist metadata

```bash
jf playlists update <PLAYLIST_ID> --name "New Name" --public
```

## Collections

### Create a collection

```bash
# Create an empty collection
jf collections create --name "My Collection"

# Create with items
jf collections create --name "Marvel Movies" --items <ITEM_ID_1>,<ITEM_ID_2>
```

### Manage collection items

```bash
# Add items to a collection
jf collections add <COLLECTION_ID> --items <ITEM_ID_1>,<ITEM_ID_2>

# Remove items (prompts for confirmation)
jf collections remove <COLLECTION_ID> --items <ITEM_ID_1>,<ITEM_ID_2>
```

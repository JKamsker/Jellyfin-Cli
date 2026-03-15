# Media Management

## Update item metadata

```bash
# Change the name of an item
jf items update <ITEM_ID> --name "New Title"

# Update multiple fields
jf items update <ITEM_ID> --name "New Title" --year 2024 --rating "PG-13"

# Set community rating
jf items update <ITEM_ID> --community-rating 8.5

# Update overview/description
jf items update <ITEM_ID> --overview "A new description for this item."
```

## Annotate a media item

A quick walkthrough: update an item's metadata, verify the change, then revert it.

```bash
# 1. Check the current state
jf items get <ITEM_ID> --json | jq '{name, communityRating}'
# → { "name": "Ajin", "communityRating": 6.2 }

# 2. Update the name, rating, and mark as favorite
jf items update <ITEM_ID> --name "Ajin (annotated)" --community-rating 9.5
jf items favorite set <ITEM_ID>

# 3. Verify
jf items get <ITEM_ID> --json | jq '{name, communityRating}'
# → { "name": "Ajin (annotated)", "communityRating": 9.5 }

# 4. Revert everything back
jf items update <ITEM_ID> --name "Ajin" --community-rating 6.2
jf items favorite unset <ITEM_ID>
```

Use `--verbose` on the update command to see which fields were changed.

## Refresh metadata

```bash
# Default refresh
jf items refresh <ITEM_ID>

# Full refresh with image and metadata replacement
jf items refresh <ITEM_ID> --mode full --replace-images --replace-metadata

# Regenerate trickplay images
jf items refresh <ITEM_ID> --regenerate-trickplay
```

## Delete an item

This removes the item from the Jellyfin library and the underlying filesystem, so the command requires confirmation or `--yes`.

```bash
# Interactive confirmation
jf items delete <ITEM_ID>

# Non-interactive
jf items delete <ITEM_ID> --yes
```

## Manage item images

```bash
# List images for an item
jf items images list <ITEM_ID>

# Upload a new primary image
jf items images set <ITEM_ID> Primary /path/to/image.jpg

# Upload a backdrop
jf items images set <ITEM_ID> Backdrop /path/to/backdrop.jpg

# Delete an image (prompts for confirmation)
jf items images delete <ITEM_ID> Primary
```

Supported image types: `Primary`, `Backdrop`, `Logo`, `Thumb`, `Banner`, `Art`, `Disc`.

Browse remote artwork before downloading it:

```bash
# List remote image providers
jf items remote-images providers <ITEM_ID>

# List candidate images
jf items remote-images list <ITEM_ID>

# Download a remote image
jf items remote-images download <ITEM_ID> --url <IMAGE_URL> --type Primary
```

## Manage subtitles

```bash
# Search subtitle providers by language
jf items subtitles search <ITEM_ID> en

# Download a remote subtitle
jf items subtitles download <ITEM_ID> <SUBTITLE_ID>

# Upload an external subtitle file
jf items subtitles upload <ITEM_ID> /path/to/subtitle.srt --language en

# Delete an external subtitle by index
jf items subtitles delete <ITEM_ID> 0
```

## Manage lyrics

```bash
# Read current lyrics for an audio item
jf items lyrics get <ITEM_ID>

# Search remote lyric providers
jf items lyrics search <ITEM_ID>

# Download remote lyrics
jf items lyrics download <ITEM_ID> <LYRIC_ID>

# Upload or delete external lyrics
jf items lyrics upload <ITEM_ID> /path/to/song.lrc
jf items lyrics delete <ITEM_ID>
```

## Mark/unmark favorites

```bash
# Mark as favorite
jf items favorite set <ITEM_ID>

# Remove from favorites
jf items favorite unset <ITEM_ID>
```

## Search external metadata providers

Search TMDb, AniDB, IMDB, and other providers configured on your server:

```bash
# Search for a series (default type)
jf items remote-search "Ajin"

# Search for a movie
jf items remote-search "The Matrix" --type Movie

# Search a specific provider only
jf items remote-search "The Matrix" --type Movie --provider TheMovieDb

# Narrow by year
jf items remote-search "The Matrix" --type Movie --provider TheMovieDb --year 1999

# JSON output for scripting
jf items remote-search "Ajin" --json
```

Supported types: `Movie`, `Series`, `BoxSet`, `Person`, `MusicArtist`, `MusicAlbum`, `MusicVideo`, `Book`, `Trailer`.

Common provider names: `TheMovieDb`, `AniDB`, `The Open Movie Database` (IMDB/OMDb).

## Download media

```bash
# Download to current directory
jf items download <ITEM_ID>

# Download to a specific path
jf items download <ITEM_ID> --output /path/to/file.mkv
```

## Find similar content and suggestions

```bash
# Find items similar to a specific item
jf items similar <ITEM_ID>

# Get suggested items for the current user
jf items suggestions
jf items suggestions --type Movie

# Show library-wide item counts
jf items counts
```

## Inspect playback and hierarchy

```bash
# Inspect media sources, codecs, and streams
jf items playback-info <ITEM_ID>

# Show the item's parent chain
jf items ancestors <ITEM_ID>

# List theme songs and theme videos
jf items themes <ITEM_ID>
```

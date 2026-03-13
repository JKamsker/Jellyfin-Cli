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

## Mark/unmark favorites

```bash
# Mark as favorite
jf items favorite set <ITEM_ID>

# Remove from favorites
jf items favorite unset <ITEM_ID>
```

## Download media

```bash
# Download to current directory
jf items download <ITEM_ID>

# Download to a specific path
jf items download <ITEM_ID> --output /path/to/file.mkv
```

# Playback Control

## List active sessions

```bash
jf sessions list

# As JSON (shows session IDs, currently playing items, etc.)
jf sessions list --json
```

## Start playback on a client

```bash
# Play an item on a session
jf sessions play <SESSION_ID> <ITEM_ID>

# Queue items to play next
jf sessions play <SESSION_ID> <ITEM_ID> --play-command PlayNext

# Add to end of queue
jf sessions play <SESSION_ID> <ITEM_ID> --play-command PlayLast
```

## Control playback

```bash
# Pause
jf sessions state pause <SESSION_ID>

# Resume
jf sessions state resume <SESSION_ID>

# Stop
jf sessions state stop <SESSION_ID>

# Next track
jf sessions state next <SESSION_ID>

# Previous track
jf sessions state prev <SESSION_ID>

# Seek to a specific position (in ticks; 10,000 ticks = 1ms)
jf sessions state seek <SESSION_ID> --position 3000000000
```

## Send a message to a client

```bash
# Display a message on the client screen
jf sessions message <SESSION_ID> "Dinner is ready!" --header "Alert" --timeout 5000
```

## Send a general command

```bash
jf sessions command <SESSION_ID> GoHome
jf sessions command <SESSION_ID> ToggleMute
jf sessions command <SESSION_ID> SetVolume
```

## Continue watching

```bash
# List items you were watching (in-progress)
jf items resume
```

# Decisions Log (so we don't drift)

Product name: **Project Vela**.

## Workflow decisions (solo dev + AI)

- Cursor is allowed to run terminal commands.
- Cursor is allowed to commit to feature branches.
- Cursor may auto-commit, but you push manually after reviewing the diff.
- Cursor Privacy Mode should be enabled for best safety.
- Concurrency rule: one Issue has one implementer lane (`impl:cursor` OR `impl:jules`) at a time.
- Jules is the default lane for implementation (one Issue -> one PR).
- Cursor is the backup lane for local run/debug, reproductions, and last-mile fixes.

## Chosen starter stack

- .NET (C#)
- Avalonia UI (cross-platform UI with Windows look + Fluent theme)
- LibVLCSharp (playback engine)
- Manual portable build (no installer)

## Non-negotiable constraints (from your answers)

- Windows 11
- Offline operation (no network required)
- Plays local files, broad format support (best-effort via LibVLC)
- Playlist ends stop by default; optional loop toggle
- Preload/cue next item (paused on first frame) with a dedicated preview window
- Preview cue mode toggle: cue **next item** (default) OR cue **selected item**
- Preview window is muted by default; optional preview audio monitoring toggle
- Two windows:
  - Control window on monitor 1 (must remain visible)
  - Output window on monitor 2 when available; otherwise share monitor 1 behind control UI
- Folder watch (local folder) including subfolders -> auto-add to playlist
- Hotkeys only when the app is focused
- Panic blackout hotkey + button (black output; audio mute default with toggle)
- Leaving panic: stay paused by default; optional setting to auto-resume if it was playing before panic
- On playback error: output goes black, operator sees error, waits for operator action
- x64 only
- Per-user installer (no admin)
- Selling/distribution should avoid licensing surprises (LGPL compliance needs a plan)

## What this repo implements now (MVP)

- Playlist + drag/drop + file picker
- Basic transport (play/pause/stop/next/prev)
- Playback speed
- Frame step forward
- Preview window + cue-next preload (paused on first frame)
- Preview audio monitoring toggle (default OFF)
- Loop playlist toggle
- Playlist persistence toggle (default ON)
- Panic resume behavior toggle (default paused)
- Folder watch add
- Panic blackout
- Error prompt (retry/skip/stop) + log file
- Screen placement best-effort + reacts to monitor changes

## What is intentionally deferred

- Reverse frame step
- Gapless playlist
- Audio output device selection
- Subtitles/captions/multi-track audio
- Advanced pre-roll / cueing features
- Folder-watch filtering UI (extension allow/deny)
- Auto-updates / update notifications / any network calling

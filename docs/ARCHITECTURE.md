# Architecture

## What this repo is

A Windows 11 desktop media player starter kit for conferences and live events (Project Vela):

- Operator/Control UI window (monitor 1)
- Output window (monitor 2 when available, otherwise shares monitor 1 behind the UI)

Core requirements implemented in the starter:

- Offline playback (local files)
- Playlist (with persistence, default ON)
- Auto-advance (stop at end by default; loop optional)
- Preview window (preload/cue next item paused on first frame; muted by default)
- Hotkeys (only when app focused)
- Folder watching (including subfolders) that auto-adds new files
- Panic Blackout (black video output + optional audio mute)
- “Never crash” posture: errors are logged and surfaced to the operator; playback stops and output goes black until the operator chooses what to do.

## High-level layers

```
ConferencePlayer.App        (Avalonia UI)
ConferencePlayer.Core       (settings, playlist model, file watching, logging)
ConferencePlayer.Playback   (IPlaybackEngine + LibVLCSharp implementation)
```

### Why this split

- **Core** can be unit tested without the UI.
- **Playback** is isolated behind `IPlaybackEngine` so you can swap engines later if needed.
- **App** focuses on wiring + UI only.

## Process model

- Two playback engine instances are used:
  - Output playback engine -> Output window video surface
  - Preview playback engine -> Preview window (silent, paused on first frame)
- Output window hosts the main video surface (LibVLCSharp.Avalonia `VideoView`).
- Preview window hosts a separate video surface used only for cueing the next item.
- Operator UI issues commands (play/pause/seek/speed/frame-step, etc.).

## Multi-monitor behavior

- If 2+ screens exist:
  - Output window is positioned on the selected screen and fills it.
  - Operator UI stays on the primary screen.
- If only 1 screen exists:
  - Output window stays behind.
  - Operator UI is set Topmost so it stays visible.

The app listens for screen topology changes to re-place the output window and keep the operator UI visible.

## Error behavior (“never crash” posture)

- Playback errors trigger:
  - Output -> black
  - Operator -> modal prompt with actions: Retry / Skip / Stop
  - Full error details written to logs

## Future expansion points

- Gapless playlist
- Preview audio (currently preview is silent)
- Output audio device selection
- Per-title color pipeline / LUT (if needed)
- Signed installer + release channels

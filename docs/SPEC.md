# Product Spec (Single Source of Truth)

This file is the authoritative product spec for **Project Vela**.

If anything conflicts between AIs, GitHub Issues, or chat logs, this file wins.

## Summary

A **Windows 11**, **offline**, **operator-driven** media playback app for conferences and live events.

Two-window model:

- **Control window** (operator UI) - stays visible on monitor 1.
- **Output window** (program output) - goes to monitor 2 when available; otherwise sits behind the control window.

## Non-goals (for MVP)

- Subtitles/captions
- Multiple audio tracks
- Gapless playback
- Reverse frame-step (explicitly optional/omitted for MVP)

## Core requirements

### R1. Playback

- R1.1 Plays local media files (offline, bundled codecs, no network initialization).
- R1.2 Must be stable: **never crash**. On error: the *Playback State Machine* must immediately transition to an **Error** state, force the output to **black** (via `IOutputController`), log the error, and then prompt the operator for action (Retry/Skip/Stop).
- R1.3 Transport:
  - Play/Pause (Hotkey: Space)
  - Stop (Hotkey: S)
  - Next/Prev (Standard transport: Plays next/prev item immediately) (Hotkeys: PageDown / PageUp)
  - Select Next/Prev (Operator cueing: Selects item without playing, updates preview) (Hotkeys: Ctrl+Right / Ctrl+Left)
  - Playback speed (Hotkeys: ] Increase, [ Decrease)
  - Frame-step forward (Hotkey: F)
  - Seek Forward/Back (10s) (Hotkeys: Shift+Right / Shift+Left)
- R1.4 Hotkeys only work when the app is focused.
- R1.5 Hotkeys are user-configurable in Settings.

### R2. Playlist

- R2.1 Playlist UI, drag-and-drop files, add files/folder, remove/clear.
- R2.2 Folder watch: watch a local folder and auto-add new media files (including subfolders).
- R2.3 Auto-advance when media ends (default ON).
- R2.4 End-of-playlist behavior:
  - Default: stop at end
  - Option: loop back to start (toggle in settings)

### R3. Preview / Cueing

- R3.1 Embedded **Preview** (in Control window) for cueing.
- R3.2 Preview preloads the next item **paused on the first frame**.
- R3.2a Cue mode (toggle in Settings or Operator UI):
  - Default: cue the **next** playlist item.
  - Option: cue the **selected** playlist item.
- R3.3 Preview is silent by default.
  - Default: muted (safety)
  - Option: enable preview audio monitoring (toggle in Settings or Operator UI)
- R3.4 Preview Transport Controls (Operator UI):
  - Play, Pause, Stop buttons for the preview player.
- R3.5 Seamless Preview:
  - Preview loading must be silent and static (no audio burst or video motion). The engine must start in a paused state or remain muted until paused.
- R3.6 Preview Panic:
  - When Panic is active, the Preview player must also be paused and muted to ensure total silence.

### R4. Panic blackout

- R4.1 Panic hotkey + UI button.
- R4.2 While panic is active:
  - The *Playback State Machine* enforces output is **black** (via `IOutputController`).
    - **Note:** This blackout command must be issued *synchronously and before* the pause command to prevent a "frozen frame" flash.
  - Audio is muted by default (toggle in settings).
  - Playback is paused (safety posture).
- R4.3 Leaving panic:
  - Default: remain paused until operator presses Play.
  - Option: auto-resume if it was playing before panic (toggle in settings).
- R4.4 Output window must remain clean (no overlays).

### R5. Packaging & distribution

- R5.1 x64 only.
- R5.2 Per-user installer (no admin).
- R5.3 Fully offline operation (installer and runtime). No update checks.
- R5.4 Commercial distribution must include a licensing compliance plan (LibVLC/LibVLCSharp is LGPL).
- R5.5 Manual updates only: do NOT implement update checks/notifications/calls.

## Acceptance (MVP)

The smoke test checklist in `docs/SMOKE_TEST.md` is the minimum acceptance suite for each release.

## Backlog (high level)

- Gapless playlist
- Output audio device selection
- Reverse frame-step
- Signed installer + update channels
- Folder-watch filtering UI (extensions/ignore patterns)

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

- R1.1 Plays local media files (offline, no network).
- R1.2 Must be stable: **never crash**. On error: show an operator-visible error, keep output black, and write logs.
- R1.3 Transport:
  - Play/Pause
  - Stop
  - Next/Prev
  - Playback speed
  - Frame-step forward
- R1.4 Hotkeys only work when the app is focused.

### R2. Playlist

- R2.1 Playlist UI, drag-and-drop files, add files/folder, remove/clear.
- R2.2 Folder watch: watch a local folder and auto-add new media files (including subfolders).
- R2.3 Auto-advance when media ends (default ON).
- R2.4 End-of-playlist behavior:
  - Default: stop at end
  - Option: loop back to start (toggle in settings)

### R3. Preview / Cueing

- R3.1 Dedicated **Preview window** for cueing.
- R3.2 Preview preloads the next item **paused on the first frame**.
- R3.3 Preview is silent by default.
  - Default: muted (safety)
  - Option: enable preview audio monitoring (toggle in settings)

### R4. Panic blackout

- R4.1 Panic hotkey + UI button.
- R4.2 While panic is active:
  - Output is black.
  - Audio is muted by default (toggle in settings).
  - Playback is paused (safety posture).
- R4.3 Leaving panic:
  - Default: remain paused until operator presses Play.
  - Option: auto-resume if it was playing before panic (toggle in settings).

### R5. Packaging & distribution

- R5.1 x64 only.
- R5.2 Per-user installer (no admin).
- R5.3 Fully offline operation (installer should not require network).
- R5.4 Commercial distribution must include a licensing compliance plan (LibVLC/LibVLCSharp is LGPL).

## Acceptance (MVP)

The smoke test checklist in `docs/SMOKE_TEST.md` is the minimum acceptance suite for each release.

## Backlog (high level)

- Gapless playlist
- Output audio device selection
- Reverse frame-step
- Signed installer + update channels

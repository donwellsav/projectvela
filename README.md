# Project Vela Starter (Windows 11 / Offline / LibVLCSharp + Avalonia)

A starter repo for an **offline** conference / live-event playback app.

Start here: `docs/SPEC.md` then `docs/WORKFLOW_AI.md`.

PDFs:
- `docs/PDF/ProjectVela_Workflow_Guide.pdf`
- `docs/PDF/ProjectVela_QuickStart_Guide.pdf`

## What this starter includes

- Two-window layout:
  - **Control window** (operator UI)
  - **Output window** (video output, black when idle/panic/error)
- Playlist (drag/drop + file picker) + persistence (default ON)
- Preview window (preloads next item paused on first frame)
- Hotkeys (active only when app focused)
- Playback speed + frame step (forward)
- Folder watch (includes subfolders) -> auto-add files
- Panic blackout (black output + optional mute, default mute)
- Leaving panic stays paused by default; optional auto-resume setting
- Auto-advance stops at end by default; optional loop setting
- Logging to per-user logs folder
- CI workflow + Release packaging workflow (Velopack)

## Run

See `START_HERE.md`.

## Hotkeys (defaults)

- Space: Play/Pause
- Enter: Play selected
- Ctrl+Left / Ctrl+Right: Previous/Next
- F: Frame step (forward)
- F12: PANIC blackout toggle

You can change these in code later.

## Docs

- `docs/ARCHITECTURE.md`
- `docs/SMOKE_TEST.md`
- `docs/WORKFLOW_AI.md`
- `docs/LICENSING.md`

## License

- This starter template: MIT (see `LICENSE`)
- LibVLC / LibVLCSharp: LGPL (see `licenses/LGPL-2.1.txt` and `THIRD_PARTY_NOTICES.md`)

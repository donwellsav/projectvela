# Smoke Test Checklist (Manual)

Run this after every PR merge and before every release.

## Setup

1. Launch the app.
2. Confirm you see:
   - Control window (operator UI) with embedded Preview area
   - Output window (black)



## Playlist persistence (default ON)

1. Add 2-3 files to the playlist.
2. Close the app.
3. Re-open the app.
4. Confirm the playlist is restored (missing files should be silently skipped).

## Playlist basics

1. Click **Add Files** and select 2-3 media files.
2. Confirm the playlist shows the files in order.
3. Select the first item and press **Enter**.
4. Confirm:
   - Output window plays video
   - Control window shows “Playing”



## Loop playlist (optional)

1. Open **Settings**.
2. Ensure:
   - Auto-advance is ON
   - Loop playlist is ON
3. Play the last item in the playlist.
4. Let it reach the end.
5. Confirm it loops back to the first item and continues playing.

## Hotkeys (only when app focused)

1. Open **Help > Keyboard Shortcuts** to view current keys.
2. Confirm the defaults:
   - **Space** (Play/Pause)
   - **S** (Stop)
   - **PageDown** (Play Next) / **PageUp** (Play Prev)
   - **Ctrl+Right** (Cue Next) / **Ctrl+Left** (Cue Prev)
   - **F** (Frame Step)
   - **F12** (Panic)
3. Go to **Settings > Shortcuts**.
4. Change **Play/Pause** to something else (e.g. **P**).
5. Save.
6. Press **P** and confirm it toggles Play/Pause.
7. Press **Space** and confirm it does *nothing* (old key unbound).
8. Change it back to **Space** and Save.
9. Click outside the app (another window) and press **Space**.
   - Confirm the app does *not* react.

## Playback speed

1. Change speed to 2.0x.
2. Confirm playback speeds up.
3. Change speed back to 1.0x.



## Preview (cue next OR cue selected)

1. Ensure "Enable Preview" is ON in Settings.
2. Add at least 2 playable files to the playlist.
3. Select the first item and press **Enter** to play.
4. Confirm:
   - Preview area (in Control window) shows the **next** item by default (paused on first frame)
   - Preview is silent by default
5. Optional: in Settings, enable "Preview audio monitoring" and confirm preview audio is audible.
6. Click **Cue Preview** and confirm it refreshes the preview.
7. In Settings, enable "Preview cues selected item (instead of next)".
8. Select a different playlist item (do not start playing it).
9. Click **Cue Preview** again and confirm preview now cues the **selected** item.

## Panic blackout

1. **Standard Panic:**
   - Start playback of a video with audio.
   - Press **F12** (Panic) or click the red "BLACKOUT (F12)" button.
   - [ ] Output window goes instantly **black**. (Verify: No frozen frame or "stuck" image is visible before blackout).
   - [ ] Audio is **muted** (silence).
   - [ ] Playback is **paused** (Status shows 'PANIC', button turns GREEN "RESUME (F12)").
   - [ ] Transport buttons (Play/Pause) work but might exit panic depending on action.

2. **Resume from Panic:**
   - Press **F12** again or click "RESUME (F12)".
   - [ ] Output window restores video immediately.
   - [ ] Audio is **restored**.
   - [ ] Playback remains **paused** (Default behavior).
   - Press Space (Play) to resume.

3. **Panic Settings Verification:**
   - Go to Settings -> General -> Panic Blackout.
   - **Uncheck** "Mute audio during panic".
   - **Check** "Resume playback automatically when leaving panic".
   - Save.
   - Start Playback -> Panic (F12).
     - [ ] Output black.
     - [ ] Audio **stops** (because of Pause).
   - Resume (F12).
     - [ ] Playback **automatically resumes** (because of "Resume playback" setting).

## Folder watching

1. In Settings:
   - enable Folder Watch
   - choose a folder
   - enable “Include subfolders”
2. Copy a new media file into the watched folder.
3. Confirm it is auto-added to the playlist.

## Multi-monitor behavior

1. Connect a 2nd monitor.
2. In Settings select that monitor for Output.
3. Confirm output moves to monitor 2 and fills the screen.
4. Disconnect monitor 2.
5. Confirm:
   - Output moves to primary screen
   - Control window stays visible on top

## Error Handling (Safety)

1. **Simulate Error:**
   - Add a corrupted/unsupported media file (or rename a text file to .mp4).
   - Attempt to play it.
2. **Verify Response:**
   - [ ] Output window goes **black** immediately. (Verify: No frozen frame or glitch visible).
   - [ ] Playback **pauses/stops**.
   - [ ] A modal dialog appears: "Critical Playback Error" (or similar).
3. **Recovery:**
   - Choose "Stop".
   - [ ] App remains stable (no crash).
   - [ ] Output remains black/logo.
   - [ ] You can load another file and play.

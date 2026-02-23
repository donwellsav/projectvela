# Manual Smoke Test Checklist

**Version:** Project Vela v1 (MVP)
**Run Frequency:** Before every release.

## 1. Setup & Launch

- [ ] **Clean Install**: Delete `%LocalAppData%\ConferencePlayer` (optional, to test fresh state).
- [ ] **Launch**: Run `ConferencePlayer.App.exe` (or `dotnet run`).
- [ ] **Verify**: Two windows appear (Control + Output).
- [ ] **Verify**: Output window is black.
- [ ] **Verify**: Control window has "Idle" status.

## 2. Playlist & File Loading

- [ ] **Add Files**: Click "Add Files" (or drag-and-drop from Explorer). Select 2-3 video files.
- [ ] **Verify**: Files appear in the playlist.
- [ ] **Verify**: Duration is calculated (e.g. "00:10").
- [ ] **Add Folder**: Click "Add Folder", select a folder with media.
- [ ] **Verify**: Files from folder (and subfolders if enabled) are added.
- [ ] **Reorder**: Drag a file in the playlist to a new position.
    - [ ] **Verify**: The order changes.

## 3. Playback Controls

- [ ] **Play**: Select first item, click "Play" (or Space).
- [ ] **Verify**: Output window shows video.
- [ ] **Verify**: Control status shows "Playing: [Filename]".
- [ ] **Pause**: Click "Pause" (or Space).
- [ ] **Verify**: Video freezes. Status "Paused".
- [ ] **Stop**: Click "Stop" (or S).
- [ ] **Verify**: Video goes black (or stops). Status "Stopped".
- [ ] **Next/Prev**: Use "Next" / "Prev" buttons.
- [ ] **Verify**: Playlist selection moves and plays immediately.
- [ ] **Seek**: Use "+10s" / "-10s".
- [ ] **Verify**: Video jumps.

## 4. Loop & Auto-Advance

- [ ] **Auto-Advance**: Play the end of a file.
- [ ] **Verify**: It automatically plays the next file.
- [ ] **Loop Toggle**: Click "Loop" toggle (ON).
- [ ] **Loop Test**: Play the last file in the playlist.
- [ ] **Verify**: After finishing, it loops back to the FIRST file and continues playing.
- [ ] **Loop Toggle Off**: Click "Loop" toggle (OFF).
- [ ] **Verify**: After finishing last file, playback stops.

## 5. Panic Mode

- [ ] **Trigger Panic**: While playing, press F12 (or click Panic button).
- [ ] **Verify**: Output immediately goes **BLACK**.
- [ ] **Verify**: Audio is muted (if configured).
- [ ] **Verify**: Playback is PAUSED.
- [ ] **Verify**: Panic button turns Green ("RESUME").
- [ ] **Resume**: Press F12 again.
- [ ] **Verify**: Output returns, audio un-mutes. Playback remains paused (default safety).

## 6. Preview Window

- [ ] **Cueing**: Select an item (don't play).
- [ ] **Verify**: Preview window shows the **next** item paused on first frame (default).
- [ ] **Cue Mode**: Switch to "Cue Selected".
- [ ] **Verify**: Preview window shows the **selected** item.
- [ ] **Preview Transport**: Click Play on Preview.
- [ ] **Verify**: Preview plays (video moves). Main output is unaffected.
- [ ] **Audio**: Toggle Preview Audio (speaker icon).
- [ ] **Verify**: You can hear preview audio.

## 7. Persistence (Full Recall)

- [ ] **Save State**: Play a video. Pause it in the middle (e.g. 00:05).
- [ ] **Close App**: Close the Control Window.
- [ ] **Relaunch**: Run the app again.
- [ ] **Verify**:
    - Playlist is restored.
    - Same item is selected.
    - Status shows "Restored: [Name] @ 00:05".
- [ ] **Resume**: Click Play.
- [ ] **Verify**: Playback resumes from 00:05.

## 8. Screen Topology

- [ ] **Multi-Monitor**: Connect a 2nd screen.
- [ ] **Verify**: Output window moves to 2nd screen automatically.
- [ ] **Disconnect**: Unplug 2nd screen.
- [ ] **Verify**: Output window moves behind Control window on primary screen.

## 9. Logging & Configuration

- [ ] **Open Logs**: Menu > View > Open Logs Folder.
- [ ] **Verify**: Explorer opens to the active log directory.
- [ ] **Verify**: A new log file exists for the current session.
- [ ] **Change Log Path**: Settings > General > Logging.
- [ ] **Action**: Browse to a new folder (e.g. `C:\Temp\Logs`).
- [ ] **Verify**: "Restart required" message appears.
- [ ] **Restart**: Close and reopen the app.
- [ ] **Verify**: Logs are now created in the new folder.
- [ ] **Verify**: "Open Logs Folder" opens the new folder.

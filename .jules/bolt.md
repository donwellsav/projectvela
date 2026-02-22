## 2026-02-22 - LibVLC Context Sharing
**Learning:** In multi-window Avalonia apps using LibVLCSharp, each `LibVLC` instance loads native libraries and allocates significant memory (~50MB+).
**Action:** Always create a single `LibVLC` instance in the composition root (App.axaml.cs) and inject it into all playback engines/players. This reduces startup time and memory footprint.

## 2026-02-22 - Folder Watch Initialization Bottleneck
**Learning:** `FolderWatchService.ScanExistingAsync` was performing synchronous file system I/O on the calling thread (UI thread), causing noticeable freezes during startup or when adding folders with many files.
**Action:** Wrapped the initial file scan logic in `Task.Run` to offload it to a background thread, ensuring the UI remains responsive even with large directories (5000+ files).

## 2026-02-22 - Zero-Allocation HashSet Lookups
**Learning:** `HashSet<string>` in .NET 9+ supports `AlternateLookup<ReadOnlySpan<char>>` via `GetAlternateLookup()`, allowing allocation-free existence checks.
**Action:** Use `GetAlternateLookup<ReadOnlySpan<char>>().Contains(span)` instead of allocating strings for dictionary/set keys when checking spans (e.g., file extensions).

## 2026-02-22 - LibVLC Context Sharing
**Learning:** In multi-window Avalonia apps using LibVLCSharp, each `LibVLC` instance loads native libraries and allocates significant memory (~50MB+).
**Action:** Always create a single `LibVLC` instance in the composition root (App.axaml.cs) and inject it into all playback engines/players. This reduces startup time and memory footprint.

# Project Vela — Cursor Rule

You are working in Project Vela, an offline Windows 11 operator media player.

Hard constraints:
- Offline-first (no network calls required during normal operation).
- Two-window model: Control UI + Output (video/black) + Preview (muted by default).
- No UI overlays over video output.
- Hotkeys only work when the app is focused.
- Panic blackout: output goes black; audio mute default ON with a settings toggle.
- On playback error: output black; show operator error; wait for operator input; log details.
- Keep changes small: one GitHub Issue -> one PR.

Process:
- Preconditions: confirm the GitHub Issue is labeled `impl:cursor` (not `impl:jules`).
- Before coding: write a short plan and list files to change.
- Use a dedicated branch: `cursor/issue-<N>-short-name`.
- Commit early and often on the feature branch.
- **Auto-commit is allowed**, but **do not push** unless the user explicitly asks.
- Default workflow: commit -> stop -> user reviews diff -> user pushes.
- Never push to `main`.
- After coding: run tests (or note how to run them) and update `docs/SMOKE_TEST.md` if behavior changed.
- Never add dependencies without updating `THIRD_PARTY_NOTICES.md` and `docs/LICENSING.md`.

Terminal:
- Terminal commands are allowed.
- Prefer safe commands: `dotnet build`, `dotnet test`, `git status`, `git diff`, `git commit`.
- If you think `git push` is needed, pause and ask the user to confirm after they review the diff.
- Record what commands were run in the PR description.

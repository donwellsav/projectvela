# AGENTS.md (for Jules + other coding agents)

This repo is designed to be *agent-friendly* and *beginner-friendly*.

## Golden rules (apply to Jules + ChatGPT + Gemini)

1. **Small, reviewable changes only.** Prefer PRs that change < 10 files and < 300 lines unless explicitly required.
2. **Never change architecture without updating docs.**
   - If you introduce a new folder or pattern, also update `docs/ARCHITECTURE.md`.
3. **No network at runtime.** This app is designed to be fully offline once installed.
4. **Stability first.** Prefer defensive programming, explicit error handling, and logging over cleverness.
5. **Don't silently swallow exceptions.** Log + show a user-facing error and pause for operator input.

## How to work in this repo (agent instructions)

When you pick up a task:

0. Confirm the GitHub Issue is claimed by you:
   - `impl:jules` => Jules (or another async agent) may implement
   - `impl:cursor` => do NOT implement (Cursor lane owns it)

1. Read:
   - `docs/ARCHITECTURE.md`
   - `docs/SMOKE_TEST.md`
   - `docs/WORKFLOW_AI.md`
2. Create a branch:
   - Jules lane: `jules/issue-<N>-<short-name>`
   - Generic agent lane: `feat/<short-name>` or `fix/<short-name>` (only if Issue is not claimed by Cursor)
3. Implement the change.
   - Committing to the feature branch is OK.
   - Do not push to `main`.
   - If you are a local agent (Cursor), do not `git push` unless the user explicitly asks.
4. Add/Update:
   - tests (where reasonable)
   - `docs/SMOKE_TEST.md` steps (if behavior changed)
5. Run:
   - `dotnet test`
   - `dotnet build`

## Definition of done (DoD)

A task is done only if:

- The app builds on Windows (`dotnet build`).
- Tests pass (`dotnet test`).
- No new warnings were introduced (or they are explained in the PR).
- Manual smoke test steps in `docs/SMOKE_TEST.md` were followed and results noted in the PR description.

## Repo conventions

- UI project: `src/ConferencePlayer.App`
- Core logic (playlist, settings, file watch): `src/ConferencePlayer.Core`
- Playback engine abstraction + LibVLC implementation: `src/ConferencePlayer.Playback`
- Tests: `tests/ConferencePlayer.Core.Tests`

## Safe defaults for agents

- Prefer adding new code behind interfaces in Core/Playback so we can swap implementations later.
- Prefer *explicit* settings with defaults defined in `AppSettings`.
- Prefer logging to `%LocalAppData%\ConferencePlayer\Logs`.

## Cursor

If you use Cursor locally, keep it aligned with this file and docs/SPEC.md. Prefer small PRs and do not bypass CI/PR checks.

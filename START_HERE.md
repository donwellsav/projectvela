# START HERE (Beginner Step-by-Step)

## What you are building (quick summary)

You are building **Project Vela**: an **offline, “operator-grade” conference playback app**:

- **Control window**: playlist + transport controls + hotkeys
- **Output window**: full-screen video output (monitor 2 when available)
- **Folder watch**: auto-add new media (including subfolders)
- **Panic blackout**: instantly black the output + mute audio (default)

## 1) Install prerequisites (Windows 11)

Pick ONE IDE:

- Visual Studio (recommended if you're totally new), OR
- VS Code, OR
- JetBrains Rider

You also need:

- **.NET SDK** (this repo targets .NET 10)
- Git

## 2) Open the project

1. Unzip the repo.
2. Open a terminal in the repo root (where `ConferencePlayer.sln` is).
3. Run:

   ```powershell
   dotnet restore
   dotnet build
   ```

## 3) Run the app (developer run)

From repo root:

```powershell
dotnet run --project .\src\ConferencePlayer.App\ConferencePlayer.App.csproj
```

You should see 2 windows:
- Control window
- Output window (black)

## 4) First smoke test

Follow: `docs/SMOKE_TEST.md`

## 5) Create a portable release

### Publish

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\publish-win.ps1
```

Your build will be in `publish/`. You can zip this folder for distribution.

## 6) How to use Jules + GitHub without chaos

Read:
- `docs/WORKFLOW_AI.md`
- `AGENTS.md`

Then: only let Jules work on **one Issue at a time** (one Issue -> one PR).

## Optional: Cursor setup (recommended)

1) Install Cursor and open the repo folder.
2) In Cursor Settings:
   - Enable **Privacy Mode** if you want reduced retention / training exposure.
   - Add repo rules (Cursor creates rule files under `.cursor/rules/`).
3) Use Cursor for *local* iterative work; still ship changes via GitHub PRs.
4) Safe Git habit: let Cursor commit to your feature branch, but **you** push after you glance at the diff.

(Details and prompts are in `docs/WORKFLOW_AI.md` and `docs/PROMPTS.md`.)

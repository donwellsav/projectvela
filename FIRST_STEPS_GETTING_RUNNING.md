# Project Vela — First Steps (Get it running)

This is the fastest path to: **clone/open -> build -> run -> first PR**.

## 0) What you should have

- Windows 11 machine
- A GitHub account
- One IDE (Visual Studio / VS Code / Rider)

## 1) Install prerequisites

1) Install the .NET SDK version required by this repo.
   - Check: `src/ConferencePlayer.App/ConferencePlayer.App.csproj` for the `TargetFramework`.

2) Install Git.

## 2) Unzip + open

1) Unzip this bundle.
2) Open a terminal in the folder that contains `ConferencePlayer.sln`.

## 3) Build

```powershell
# from repo root

dotnet restore

dotnet build
```

## 4) Run

```powershell
# from repo root

dotnet run --project .\src\ConferencePlayer.App\ConferencePlayer.App.csproj
```

Expected: Control window + Output window (black).

## 5) Quick smoke test

Follow: `docs/SMOKE_TEST.md`

## 6) Make your first GitHub repo

1) Create a new repo on GitHub (empty).
2) In your local folder:

```powershell
git init
git add .
git commit -m "Initial import: Project Vela starter"

# add your remote and push
# git remote add origin <YOUR_REPO_URL>
# git push -u origin main
```

## 7) Turn on guardrails (recommended)

- Enable a protected `main` branch.
- Require PRs and passing checks before merge.

## 8) Set up AI lanes

### Jules (default implementer)

- Create a GitHub Issue with acceptance criteria.
- Add the label `jules` to start a task.

### Cursor (debug / last-mile)

- Open the repo in Cursor.
- Confirm `.cursor/rules/project-vela.md` exists.
- Cursor may commit on `cursor/issue-<N>-...` branches.
- You push manually after reviewing diffs.

### ChatGPT + Gemini

Use:
- `docs/SPEC.md`
- `docs/DECISIONS.md`
- `docs/WORKFLOW_AI.md`
- `docs/PROMPTS.md`

And the PDF prompt playbook:
- `docs/PDF/ProjectVela_Prompt_Playbook_v0.9.3.pdf`

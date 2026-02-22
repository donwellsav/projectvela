# AI Workflow (Cursor + Gemini + ChatGPT + Jules + GitHub)

This workflow is written for a **solo developer** who is brand-new to coding and wants to use multiple AI tools without drift.

## Goals

1. Reduce contradictions and “AI vs AI” drift.
2. Keep changes small and reviewable.
3. Make automation (tests + CI + packaging) catch mistakes.
4. Prevent Cursor and Jules from editing the same files at the same time.

## The most important rule

**One GitHub Issue -> one implementer -> one PR at a time.**

That means:

- Every change has a GitHub Issue.
- The Issue is claimed by either Cursor or Jules.
- Only one PR is open for that Issue.

## Cursor vs Jules (what each tool is best for)

### Default recommendation (best chance of success)

- **Jules** = primary implementer for most work (one Issue -> one PR).
- **Cursor** = backup implementer for local run/debug and last-mile fixes that require hands-on Windows behavior.

### Use Cursor when you need any of these

- run the app locally and inspect behavior
- reproduce a bug with real files
- debug multi-monitor behavior on Windows
- learn by “pair programming” while you build

### Use Jules when the task is

- well-scoped (clear acceptance criteria)
- mostly edits to docs/tests/refactors
- safe to run unattended

## Work-Claiming Protocol (prevents conflicts)

### Step 1 — Claim the Issue

In GitHub, pick one **lane** label:

- `impl:cursor` (you will implement locally in Cursor)
- `impl:jules` (Jules will implement and open a PR)

Rule: never put both labels on the same Issue.

If you want **Jules** to actually start working from a GitHub Issue, also add the label:

- `jules` (case-insensitive)

This label is what triggers the Jules GitHub integration.

### Step 2 — Create a dedicated branch

Branch naming:

- Cursor branches: `cursor/issue-<N>-short-name`
- Jules branches: `jules/issue-<N>-short-name`

### Step 3 — Declare the “touch list”

In the Issue body (or first comment), list:

- files you expect to change
- files you promise not to touch

If an implementer needs more files, update the Issue before changing them.

### Step 4 — Do not work on the same files simultaneously

If you want to use both tools on the same feature:

1) run Cursor first for the local behavior spike
2) merge
3) then run Jules for follow-up tests/docs/refactors

## Cursor terminal commands: allowed, but keep it safe

You chose to allow Cursor Agent mode to run terminal commands freely.

Recommended safe default commands:

- `dotnet build`
- `dotnet test`
- `dotnet run --project ...`
- `git status`, `git diff`, `git commit` on feature branches

### Commit automatically, push manually (your rule)

You said: **Cursor may commit automatically**, but you will **push manually after you glance at the diff**.

Do this every time:

1) **Cursor** runs:
   - `git status`
   - `git diff`
   - `dotnet test`

2) **Cursor** creates a commit on the feature branch:
   - commit message: `Issue <N>: <short summary>`

3) **Cursor stops** (no push).

4) **You** do a fast human check:
   - `git show` (or GitHub Desktop / VS Code diff)
   - sanity check: “does this match the Issue + touch list?”

5) **You** push:
   - `git push -u origin <branch>`

Why we do this: it reduces “oops I pushed junk” moments and still keeps automation fast.

Always record what commands were run in the PR description.


## Privacy / safety recommendation

Enable Cursor Privacy Mode for this repo.

## The “single source of truth” rule

Keep one authoritative place for product requirements and decisions:

- `docs/SPEC.md` (product requirements)
- `docs/DECISIONS.md` (decisions log)
- `docs/ARCHITECTURE.md` (architecture)
- `docs/SMOKE_TEST.md` (manual acceptance steps)
- `docs/LICENSING.md` (LGPL compliance plan)
- GitHub Issues (each feature/bug is an Issue with acceptance criteria)

Every AI prompt should reference the relevant docs by path.

## Recommended roles for each AI

### ChatGPT (5.2 Pro)

Use for:

- architecture decisions
- risk review
- writing acceptance criteria + checklists
- PR review (paste diffs)
- merge-conflict resolution help

### Gemini (Ultra / Deep Think)

Use for:

- adversarial “red-team” review
- edge cases + stability risks
- alternative designs

### Cursor

Use for:

- local debugging and stepping through behavior
- reproducing bugs with real media files
- last-mile fixes after a Jules PR (in a separate Issue/PR)

### Jules

Use for:

- implementing a single Issue with strict constraints
- writing tests and docs updates
- mechanical refactors that can run unattended

## The 5-step loop (recommended)

1) Write the Issue
   - clear goal
   - explicit non-goals
   - defaults + toggles
   - acceptance criteria

2) Pick the implementer
   - add `impl:cursor` or `impl:jules`
   - create a feature branch

3) Plan (ChatGPT + Gemini)
   - propose file changes
   - identify risks
   - define verification (tests + smoke steps)

4) Implement (Cursor or Jules)
   - keep PR small
   - add tests or explain why not

5) Verify and merge (you)
   - run `dotnet test`
   - run `docs/SMOKE_TEST.md`
   - review PR diff and merge

## GitHub templates

This repo includes:

- `.github/ISSUE_TEMPLATE/feature.yml`
- `.github/ISSUE_TEMPLATE/bug.yml`
- `.github/pull_request_template.md`

Use these so AIs don’t invent requirements.

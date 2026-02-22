# Project Vela — Prompt Library (Cursor + Jules + ChatGPT + Gemini)

This file is **copy/paste friendly**. It is written for a **beginner** who wants “automation without chaos”.

> Tip: if a prompt ever feels too long, keep the *Universal Header* + the *Output Format* sections.
> They are the parts that prevent drift.

---

## How to use this library (beginner, error-proof)

### The golden loop (use this every time)

1) **Write a GitHub Issue first**
   - Put goal, non-goals, defaults/toggles, and acceptance checks.
2) **Pick ONE implementer lane**
   - `impl:cursor` OR `impl:jules` (never both).
3) **Plan in ChatGPT**
   - Get a small plan + touch list + test plan.
4) **Red-team in Gemini**
   - Try to break the plan; add missing edge cases.
5) **Implement in Cursor OR Jules**
   - Keep the PR small.
6) **You verify**
   - Run tests, run smoke tests, glance at diff.
7) **Merge + package**

### The “don’t get burned” rules

- **One Issue → One PR.** If it gets big, split it.
- **Touch list required.** If the AI needs more files, update the Issue *before* changing them.
- **Auto-commit is OK, auto-push is not.** Cursor can commit; you push after a quick diff review.
- **Never-crash posture is sacred.** On error: output black + operator prompt + logs.
- **No new dependencies unless you explicitly allow it.**
- If the AI cannot verify something, it must say **“I am uncertain about this”** and ask for the missing info.

---

## Universal Header (paste into EVERY prompt)

Replace the placeholders in ALL CAPS.

```text
You are working on Project Vela (Windows 11 offline media player).

Repo docs you must follow:
- docs/SPEC.md
- docs/DECISIONS.md
- docs/ARCHITECTURE.md
- docs/SMOKE_TEST.md
- docs/LICENSING.md

Hard constraints (do not violate):
- Fully offline operation once installed.
- Two-window model: Control UI + Output window (black when idle/panic/error) + Preview window (muted by default).
- No UI overlays over video output.
- Hotkeys work ONLY when app is focused.
- Panic blackout: output black; audio mute default ON; setting can toggle.
- On playback error: output black; show operator error; WAIT for operator input; write logs.

Process constraints:
- One GitHub Issue -> one PR.
- Respect the Issue “touch list” (files allowed to change).
- No new dependencies unless explicitly permitted.
- Keep diffs small (~300 LOC) unless Issue says otherwise.

IMPORTANT:
- Do not guess. If you cannot verify, say “I am uncertain about this”.
```

### Output Format (paste after the header)

```text
Output exactly these sections:

1) Summary (1-3 sentences)
2) Plan (6-10 steps max)
3) Touch list (files to change)
4) Risk list (what could break)
5) Verification
   - Unit tests to add/update
   - Smoke test steps (add to docs/SMOKE_TEST.md)
6) If uncertain (list unknowns and what you need)
```

---

# 1) ChatGPT 5.2 Pro prompt set (planning + review)

## 1.1 Turn an idea into a GitHub Issue (feature)

**Use this when:** you have a feature idea but no crisp Issue yet.  
**You provide:** your rough idea + any UI notes.  
**You get:** a ready-to-paste GitHub Issue with acceptance criteria + toggles.

```text
{"Universal Header"}

Create a GitHub Issue for this feature idea:

IDEA:
<PASTE YOUR IDEA>

Include:
- Goal
- Non-goals
- Defaults and toggles (explicit)
- Acceptance criteria (testable)
- A “touch list” guess (files likely to change)
- A rollback plan (how to disable if unstable)
```

## 1.2 Decide the implementer lane (Cursor vs Jules)

**Use this when:** you want to avoid Cursor/Jules conflicts.  
**You get:** lane decision + touch list.

```text
{"Universal Header"}

Given this GitHub Issue:

<PASTE ISSUE>

Decide:
- Lane: impl:cursor OR impl:jules
- Why

Then output:
- Touch list (files expected to change)
- Risk list
- Test plan
- A short implementer prompt (tailored to the chosen lane)
```

## 1.3 Write a “mini design doc” (Architecture + pitfalls)

**Use this when:** the Issue touches playback, multi-monitor, or file watching.

```text
{"Universal Header"}

Write a 1-page design note for Issue <N>:

<PASTE ISSUE>

Include:
- Proposed approach (high level)
- Failure modes + recovery behavior (never-crash posture)
- Settings additions (key + default)
- What NOT to do (anti-patterns for this repo)
- Smoke test updates needed
```

## 1.4 Create a test plan that a beginner can run

**Use this when:** you want a “do this exactly” checklist.

```text
{"Universal Header"}

Create a beginner-friendly test plan for Issue <N>.

Inputs:
- Issue: <PASTE>
- Current smoke test: <PASTE RELEVANT SECTION FROM docs/SMOKE_TEST.md>

Output:
- Unit tests to add/update (names + what to assert)
- Manual smoke test steps (exact clicks/hotkeys)
- Expected results
- Failure handling: what the app should do if the test fails (operator messaging + logs)
```

## 1.5 PR review (copy/paste diff)

**Use this when:** Cursor/Jules opened a PR and you want a review pass.

```text
{"Universal Header"}

Review this PR diff for compliance with docs/SPEC.md and never-crash posture.

PR description:
<PASTE PR DESCRIPTION>

Diff:
<PASTE DIFF OR FILE LIST + KEY CHANGES>

Output:
- Blockers (must fix)
- Important issues
- Nice-to-have
- Missing tests
- Any spec drift you detect (quote the SPEC section by heading)
```

## 1.6 Merge conflict helper

```text
{"Universal Header"}

Help me resolve this Git merge conflict.

Constraints:
- Keep changes minimal.
- Preserve behavior required by docs/SPEC.md.

Inputs:
- Conflict file path:
- Conflict markers:
<PASTE CONFLICT>

Output:
- Resolved file content
- Explanation (short)
- What to re-test
```

## 1.7 Release checklist (before you ship to real users)

```text
{"Universal Header"}

Create a release checklist for Project Vela <VERSION>.

Include:
- Build + tests
- Smoke tests
- Packaging with Velopack
- Verifying “per-user no-admin” install behavior
- Log folder verification
- License compliance checklist (LGPL notices included)
- What to write in release notes (template)
```

---

# 2) Cursor prompt set (implementation + debugging)

> Cursor is your **local** implementer. It shines when you need to *run the app* and iterate.

## 2.1 Implement an Issue (auto-commit allowed, NO push)

**Use this when:** Issue is labeled `impl:cursor`.

```text
{"Universal Header"}

Implement Issue #<N>.

Preconditions:
- Confirm Issue label is impl:cursor.
- Create branch: cursor/issue-<N>-<short-name>.
- Confirm touch list: <PASTE TOUCH LIST>

Constraints:
- No new dependencies.
- Keep changes under ~300 LOC.
- Preserve never-crash posture.

Steps:
1) Write a short plan (max 8 bullets).
2) Make changes (small commits are OK).
3) Run:
   - dotnet test
4) Create ONE final commit (squash if needed):
   - Message: Issue <N>: <summary>
5) STOP. Do NOT push. Tell the human:
   - what changed
   - what commands you ran
   - how to smoke test
```

## 2.2 Fix a bug from logs (smallest fix first)

```text
{"Universal Header"}

I can reproduce a bug by doing:
<STEPS>

Log snippet:
<PASTE>

Please:
- Identify likely root cause (point to file + function)
- Propose the smallest safe fix
- Add/extend logging so the next failure is diagnosable
- Update docs/SMOKE_TEST.md with a repro step (if appropriate)
```

## 2.3 Add a new setting / toggle (with defaults)

```text
{"Universal Header"}

Add a new setting toggle:

- Setting key name: <NAME>
- Default: <TRUE/FALSE>
- UI location: Settings menu
- Behavior: <DESCRIBE>

Requirements:
- Persist in the existing settings mechanism.
- Add to docs/SPEC.md “Defaults and toggles”.
- Add a smoke test step.

Output:
- Files changed
- How to verify
- Commit message suggestion
```

## 2.4 Implement “panic blackout” behavior changes safely

```text
{"Universal Header"}

Implement the following panic blackout behavior:

<PASTE ISSUE OR REQUIREMENTS>

Constraints:
- Output window must go black.
- Audio mute default ON (toggle exists).
- Leaving panic: default is paused; optional auto-resume toggle.

Make the smallest change that meets acceptance criteria.
Add logs for panic transitions and playback state.
```

## 2.5 Implement playlist behavior safely (auto-advance/loop)

```text
{"Universal Header"}

Implement playlist behavior from this Issue:

<PASTE ISSUE>

Ensure:
- Default: auto-advance stops at end
- Optional: loop back to top (toggle)
- No crashes on invalid media: show error + wait for operator

Add smoke tests for:
- normal end of media
- end of playlist
- loop toggle on/off
```

## 2.6 Preload/Cue next item in preview (paused on first frame)

```text
{"Universal Header"}

Implement: “preload/cue next item (paused on first frame) with a dedicated Preview window.”

Constraints:
- Preview window is always muted by default.
- Optional “Preview audio” toggle in settings.
- Preview must NEVER steal focus from Control UI.
- On error: preview should fail gracefully without crashing playback.

Output:
- Plan
- Files touched
- How to test quickly with sample media
```

## 2.7 Pre-push checklist (human review helper)

**Use this when:** Cursor is done and you (human) are about to push.

```text
You are my pre-push checklist.

Given:
- Issue text: <PASTE>
- git diff: <PASTE>

Check:
- Does the diff match the Issue goal + touch list?
- Any spec drift vs docs/SPEC.md?
- Any missing error handling?
- Any new deps?
- Are we still offline-first?

Output:
- Push/No-push recommendation
- Any fixes before push
```

---

# 3) Google Jules prompt set (async PRs)

> Jules is best when the Issue is well-scoped and you want an async PR.

## 3.1 Implement a single Issue (small PR)

```text
{"Universal Header"}

Implement GitHub Issue #<N>.

Preconditions:
- Confirm Issue label is impl:jules.
- Work only inside this touch list:
<PASTE TOUCH LIST>

Constraints:
- Keep changes under ~300 LOC.
- No new dependencies.
- Update docs/SMOKE_TEST.md if behavior changes.
- Add or update unit tests where possible.

Deliverables:
- A PR with:
  - clear description
  - commands run
  - test plan
```

## 3.2 “Docs only” PR (safe Jules task)

```text
{"Universal Header"}

Task: Update documentation only.

Goal:
- Make docs/SMOKE_TEST.md clearer for beginners.
- Ensure docs/SPEC.md includes all toggles and defaults.

Constraints:
- Do not change production code.
- Keep PR small.

Deliverable:
- PR with documentation improvements.
```

## 3.3 “Tests only” PR (safe Jules task)

```text
{"Universal Header"}

Task: Add unit tests for <COMPONENT>.

Constraints:
- Do not change production code unless required for testability.
- If a refactor is needed, explain why and keep it minimal.

Deliverable:
- PR with tests and a short explanation of what is now covered.
```

## 3.4 “Refactor only” PR (mechanical cleanup)

```text
{"Universal Header"}

Task: Refactor <AREA> for readability and stability.

Constraints:
- No behavior change.
- No new dependencies.
- Keep changes small.

Deliverable:
- PR with before/after explanation and how you verified behavior did not change.
```

---

# 4) Gemini Ultra / Deep Think prompt set (red-team)

## 4.1 Red-team a feature plan

```text
Act as a red-team reviewer for Project Vela.

Given this plan:
<PASTE PLAN>

And these constraints:
<PASTE KEY SPEC SECTIONS>

Attack the plan:
- crash risks
- format edge cases
- multi-monitor pitfalls
- file watcher pitfalls
- operator workflow hazards

Output:
- failure modes
- mitigations
- missing acceptance checks
```

## 4.2 Stability/performance experiment checklist (offline)

```text
For Project Vela (Avalonia + LibVLCSharp), list top stability/performance risks and how to test them offline.

Output a prioritized checklist of experiments (each < 30 minutes):
- what to do
- what log lines to look for
- pass/fail criteria
```


---

# 5) Extra prompts (when you want to go deeper)

These are “second-line” prompts you use after the top set.

## 5.1 ChatGPT — Spec drift checker (fast)

**Use this when:** you suspect an AI changed behavior away from the spec.

```text
You are checking for spec drift in Project Vela.

Inputs:
- docs/SPEC.md excerpt: <PASTE>
- PR diff or description: <PASTE>

Task:
- List every possible way the PR conflicts with the spec.
- If unsure, say: "I am uncertain about this" and ask for the missing file content.

Output:
- Drift list
- Fix suggestions (minimal)
```

## 5.2 ChatGPT — PR description writer (so reviewers trust it)

```text
Write a PR description for Issue #<N>.

Inputs:
- Issue: <PASTE>
- Key commits summary: <PASTE>
- Commands run: <PASTE>
- Smoke test steps run: <PASTE>

Output:
- Summary
- What changed (bullets)
- Verification (commands + manual)
- Risk notes
- Rollback plan
```

## 5.3 ChatGPT — Release notes (public-friendly)

```text
Write release notes for Project Vela v<VERSION>.

Inputs:
- Merged PR list (titles): <PASTE>
- Known issues: <PASTE>
- Any breaking changes: <PASTE>

Output:
- Highlights (3-6 bullets)
- Fixes
- Known issues
- Upgrade notes
```

## 5.4 Cursor — Add a hotkey safely (focused-only)

```text
{{"Universal Header"}}

Add a new hotkey:
- Key: <KEY>
- Action: <ACTION>
- Requirement: works only when app is focused (no global hotkeys)

Also:
- Update docs/SMOKE_TEST.md with a hotkey test step
- Add logs for the action
```

## 5.5 Cursor — Improve “never crash” posture (error-path hardening)

```text
{{"Universal Header"}}

Harden error handling for <AREA>.

Goal:
- Replace any crashy behavior with:
  - output black
  - operator-visible error
  - logs written

Constraints:
- minimal changes
- no new dependencies

Output:
- list of risky call sites you changed
- how to reproduce to verify
```

## 5.6 Jules — CI improvements (non-product change)

```text
{{"Universal Header"}}

Task: improve CI reliability and speed.

Constraints:
- Do not change product behavior
- Keep PR small

Ideas you may consider:
- caching NuGet
- clearer CI logs
- fail-fast steps

Deliverable:
- PR with explanation
```

## 5.7 Gemini — “break the release” checklist

```text
Act as a QA lead for an offline Windows media player.

Given these features:
- panic blackout
- preview window
- folder watch (subfolders)
- playlists + loop toggle

Create a brutal “break it” checklist:
- 20 short tests
- expected behavior
- what logs to check
```

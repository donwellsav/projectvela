# Project Vela — Master Cookbook (v0.6.0)

This is the **long-form** guide for how to build Project Vela with a multi-AI workflow:
Cursor + Google Jules + ChatGPT 5.2 Pro + Gemini Ultra + GitHub.

- For the shorter version, see `docs/PDF/ProjectVela_QuickStart_Guide_v0.6.0.pdf`
- For the workflow-focused version, see `docs/PDF/ProjectVela_Workflow_Guide_v0.6.0.pdf`

This Markdown file is a companion to the PDF version.

## Contents

1. Product summary (requirements + defaults)
2. Repo map
3. Workflow (one issue -> one PR)
4. Tool roles (who does what)
5. Conflict prevention (Cursor vs Jules)
6. Automation (CI, packaging, releases)
7. Prompt library (see `docs/PROMPTS.md`)
8. Beginner troubleshooting

## Quick reminders

- **Never crash.** On error: black output + operator prompt + logs.
- **Offline-first.** No network required for playback.
- **Auto-commit OK; push after you review diff.**
- **Only ONE implementer lane per Issue.**

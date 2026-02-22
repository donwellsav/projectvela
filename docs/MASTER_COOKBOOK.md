# Project Vela — Master Cookbook (living guide)

This is the **long-form** guide for how to build Project Vela with a multi-AI workflow:
Cursor + Google Jules + ChatGPT 5.2 Pro + Gemini Ultra + GitHub.

PDFs in this repo:

- Quick Start: `docs/PDF/ProjectVela_QuickStart_Guide_v0.6.0.pdf`
- Workflow Guide: `docs/PDF/ProjectVela_Workflow_Guide_v0.6.0.pdf`
- Master Cookbook (full): `docs/PDF/ProjectVela_Master_Cookbook_0.8.0.pdf`
- Prompt Playbook (full): `docs/PDF/ProjectVela_Prompt_Playbook_v0.9.3.pdf`

This Markdown file is a companion to the PDF version.

## Contents

1. Product summary (requirements + defaults)
2. Repo map
3. Workflow (one issue -> one PR)
4. Tool roles (who does what)
5. Conflict prevention (Cursor vs Jules)
6. Automation (CI, packaging, releases)
7. Prompt library (see `docs/PROMPTS.md` + the Prompt Playbook PDF)
8. Beginner troubleshooting

## Quick reminders

- **Never crash.** On error: black output + operator prompt + logs.
- **Offline-first.** No network required for playback.
- **Auto-commit OK; push after you review diff.**
- **Only ONE implementer lane per Issue.**

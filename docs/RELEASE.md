# Release / Installer (Velopack)

This repo uses Velopack for creating a per-user Windows installer.

## One-time setup (on your machine)

1. Install .NET SDK (see START_HERE.md).
2. Install the Velopack CLI tool:

   `dotnet tool install -g vpk`

## Build + publish (win-x64)

From repo root:

1. `powershell -ExecutionPolicy Bypass -File .\scripts\publish-win.ps1`

This creates a `publish/` folder containing the app build output.

## Package with Velopack

1. `powershell -ExecutionPolicy Bypass -File .\scripts\pack-velopack.ps1 -Version <VERSION>`

   Use your chosen app version, e.g. `-Version 1.0.0`.

This creates a `Releases/` folder containing (names depend on PackId and version):

- `ProjectVela-Setup.exe` (installer)
- `ProjectVela-Portable.zip` (portable)
- `ProjectVela-<version>-full.nupkg` (full package)

## Offline distribution

If you do not want auto-update:

- Distribute only the `*-Setup.exe` installer (and/or the portable zip).
- Do not host any update feed.
- Do not add UpdateManager network calls in the app.

## Code signing (recommended for real users)

See Velopack docs for signing support.

# Release (Portable)

This repo produces a **portable** (zip) distribution. It does **not** use an installer or auto-updater.

## Build + publish (win-x64)

From repo root:

1. `powershell -ExecutionPolicy Bypass -File .\scripts\publish-win.ps1`

This creates a `publish/` folder containing the app build output.

## Package (Portable Zip)

The publish script above automatically creates a zip file if you are using the standard CI workflow, or you can manually zip the `publish` folder.

To create a clean portable release:

1. Run the publish script.
2. Navigate to `publish/`.
3. Zip the contents.
4. Distribute the zip file.

## Offline distribution

The app is fully offline.

- Distribute the portable zip.
- No update checks are performed.
- No network calls are made.

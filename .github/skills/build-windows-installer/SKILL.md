---
name: build-windows-installer
description: "Use when publishing Tibia Hunt Analyzer for Windows or building its Inno Setup installer executable. Covers prerequisites, output cleanup, build order, and artifact verification."
argument-hint: "Build the Windows publish output or installer."
user-invocable: true
---

# Build the Windows Installer

Use the repository's existing scripts; do not create a second packaging flow.

## Prerequisites

- Run on Windows with the .NET 10 SDK.
- Inno Setup must be installed. `scripts/build-installer.ps1` searches PATH and common installation locations for `ISCC.exe`.
- Do not bundle a user's SQLite database or configuration. They live under the user's application-data directory, not in the publish output.

## Procedure

1. Inspect `scripts/publish-windows.ps1`, `scripts/build-installer.ps1`, and `installer/TibiaHuntAnalyzer.iss` if packaging requirements or settings are changing.
2. Before publishing, inspect `artifacts\windows\win-x64`. The publish script removes that entire output directory before rebuilding; keep no unrelated or user-created files there. From the repository root, publish the current source:

   ```powershell
   .\scripts\publish-windows.ps1
   ```

   The installer script reuses an existing `artifacts\windows\win-x64` folder, so running the publish step first prevents packaging stale binaries.
3. Before invoking the installer script, inspect `artifacts\windows\installer`. The script removes that entire output directory before compiling; keep no unrelated or user-created files there.
4. Build the installer:

   ```powershell
   .\scripts\build-installer.ps1
   ```

5. Verify the command succeeds and that `artifacts\windows\installer\TibiaHuntAnalyzer-Setup.exe` exists with a current timestamp. Report the actual output path and any compiler errors.

## Boundaries

- Building the setup file does not install or launch the application. Do not install it on the current machine unless explicitly requested.
- Do not sign binaries or claim a verified publisher unless the user explicitly requests signing and an appropriate certificate is configured.
- If Inno Setup is unavailable, report the missing prerequisite; do not silently skip installer compilation or present the publish output as an installer.

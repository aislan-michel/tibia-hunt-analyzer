# Feature Changes — Implementation Tasks

## Phase 1: Inspect and specify

- [x] Inspect existing projects, dependencies, navigation, settings persistence, database startup, and CSV components.
- [x] Update the product specification for dedicated entity management, database-path settings, return navigation, and the reduced interchange scope.
- [x] Document the proposed database initialization strategy: validate before atomic config persistence; use a single startup-resolved path; activate path changes after restart.
- [x] Record implementation sequence, affected areas, risks, and verification below.
- [x] Obtain approval of this implementation plan before changing application code.
- [x] Audit the .gitignore behavior for backup directories under `src` and narrow the exclusion so source-tree backup folders remain visible while repo-level backup exclusions continue to work.

### Phase 1 audit findings

- The broad `.gitignore` rule `Backup*/` at `.gitignore:266` was ignoring `src/TibiaHuntAnalyzer.Infrastructure/Backups/DatabaseBackupService.cs` as confirmed by `git check-ignore -v`.
- The `Backups` folder is part of the application source tree and must remain tracked, so the source-tree exclusion must be narrowed with an explicit unignore rule under `src`.
- The navigation model is mostly consistent: home screen shutdown is handled by `MainViewModel.ApplicationExitRequested`, while secondary screens return to the previous VM via `ExitRequested` handlers.
- Character and hunt-location CRUD remain separated from settings and from the scan history persistence flow; the current architecture matches the requested partitioning.
- The repository does not currently show stale CSV implementation code, but the project should continue to audit for obsolete interchange references before any further refactors.

## Phase 2: Implement after approval

1. **Return navigation** — Complete
   - Change insertion-screen Sair to return to the prior view, preserving its existing unsaved-input confirmation.
   - Ensure screen-level Back/Exit actions return to their caller; retain application shutdown only at the application/window boundary.
2. **Dedicated CRUD** — Complete
   - Add direct home navigation to character management and hunt-location management.
   - Reuse soft deactivation and persistence semantics; provide separate focused screens and preserve historical session associations.
   - Reduce the settings screen to database path configuration.
3. **Database path configuration** — Complete
   - Add stable per-user configuration storage and resolve one active path before constructing database-dependent services.
   - Validate new targets and existing application databases without overwriting existing files; initialize a new database through reviewed EF migrations.
   - Persist a candidate only after validation, report that changes activate after restart, and provide an actionable startup error for an unusable configured database.
4. **Remove spreadsheet interchange** — Complete
   - Remove CSV UI, application contracts/results, infrastructure implementation and registration, tests, package reference, and CSV-specific documentation.
   - Preserve JSON/text imports, SQLite data, queries, analytics, and backup/restore.
5. **Documentation and cleanup** — Complete
   - Reconcile product specification and implementation notes with actual behavior.
   - Search for obsolete commands, registrations, dependencies, and user-facing claims.

## Phase 3: Verification

- [x] Run focused database-path unit and SQLite integration tests, including invalid database rejection and failed path-change safety.
- [x] Run the complete Windows solution build and unit/integration test suites.
- [x] Search source, tests, project documentation, and manifests for obsolete spreadsheet-import/export surfaces.
- [x] Restore dependencies and run the complete solution build and test suites under Ubuntu/Linux.
- [ ] Smoke-launch the desktop app on Windows and in a Linux graphical session when available.

## Verification results

- Windows/.NET SDK 10.0.300: solution build succeeded with 0 warnings and 0 errors; 79 unit and 17 integration tests passed.
- Ubuntu 24.04/.NET SDK 10.0.106: solution build succeeded with 0 warnings and 0 errors; 79 unit and 17 integration tests passed.
- Source, test, project-manifest, and product-specification searches found no remaining CSV import/export surfaces.
- Interactive visual smoke-launch remains unverified.

## Phase 4: Windows publish script audit and plan

- [ ] Confirm the exact application project, runtime target, and runtime dependency assumptions for Windows publication.
- [ ] Decide whether the published app should be self-contained and whether single-file packaging is appropriate for the Avalonia desktop app.
- [ ] Add a repeatable Windows PowerShell publication script under `scripts/` that resolves the repo root reliably and publishes `Release`/`win-x64` to a predictable output directory.
- [ ] Verify the published `.exe` exists, the output folder contains required runtime files, and the script returns a nonzero code when publishing fails.
- [ ] Document prerequisites, output directory, launch instructions, and any runtime limitations in the repository documentation.

### Audit findings for publication

- The concrete desktop entry project is `src/TibiaHuntAnalyzer.Desktop/TibiaHuntAnalyzer.Desktop.csproj`.
- It targets `net10.0` and emits a `WinExe` using Avalonia Desktop and Avalonia themes/assets; this is a standard native Windows desktop app and does not require a development server or a subsequently running `dotnet run` host.
- The app stores its database in the per-user `LocalApplicationData\TibiaHuntAnalyzer` folder via `ApplicationDataPaths.GetDefaultDatabasePath()`, and it persists a separate config file with the chosen database path; this is suitable for a published app so long as the executable is not forced to run from a protected install directory.
- The project is already configured as a desktop app with `AvaloniaResource` assets and `app.manifest`; this makes single-file publishing a secondary concern rather than an unconditional choice because it must be validated against Avalonia resources, native dependencies, and runtime file access.
- The repository currently has `scripts/run.sh`, `scripts/build.sh`, and `scripts/test.sh`; a Windows publication script should follow the existing scripts directory conventions and output a predictable `artifacts/windows/win-x64/` or similar folder.
- A `README.md` does not currently exist in the repository, so the publishing instructions should be added to a new or existing user-facing documentation file if approved.

## Design notes and risks

- The existing context factory is configured once at startup. The plan avoids runtime factory replacement and requires a restart after a path change; the settings UI must make that explicit.
- Startup currently applies EF migrations before Avalonia is displayed. The implementation must ensure corrupt or incompatible configured databases produce a recoverable, actionable message rather than an opaque crash or silent fallback.
- A candidate new database may be initialized before its path setting is committed. If configuration persistence then fails, the unreferenced new file may remain, but the active database and saved configuration remain unchanged.
- Entity renames preserve historical identity through existing foreign-key references; deactivation remains soft and must never delete referenced entities.
- Removing CSV code must not change the SQLite schema or modify stored session data.

## Phase 5: Branding, versioning, and Windows executable identity

- [ ] Centralize the application version at `1.0.0` in the desktop project metadata and read it from the assembly during runtime.
- [ ] Add a shared footer to the application shell with the attribution `created by Aislan Michel` and a visually secondary `1.0.0` version line.
- [ ] Make only the creator name clickable and point it to `https://github.com/aislan-michel`.
- [ ] Confirm the existing ICO file at `src/TibiaHuntAnalyzer.Desktop/Assets/tibia-hunt-analyzer.ico` is used for the Windows app icon and published executable.
- [ ] Adjust the Windows publish workflow so the final executable name is `Tibia Hunt Analyzer.exe` while preserving the runtime files required by the app.
- [ ] Update the README files and relevant task documentation to reflect the finalized branding/version/publish behavior.
- [ ] Validate the build, automated tests, and Windows publish output in the available environment.

### Phase 5 notes

- The best shared integration point for the footer is the application shell, not each screen view, because all screens are already hosted under a single `MainWindow` and `MainViewModel` shell.
- The app version should come from the project metadata instead of a duplicated hard-coded text value in the UI to reduce drift.
- The published Windows executable name must be adjusted after the .NET publish step so the user-facing file is easy to identify and open without using the terminal.
- The app icon should be configured at the project level and remain consistent with the shell window icon to provide correct Windows Explorer branding.

## Phase 6: Windows installer packaging (pending approval)

- [ ] Confirm the installer requirements against the current app metadata: version `1.0.0`, published executable name `Tibia Hunt Analyzer.exe`, and published output directory `artifacts/windows/win-x64`.
- [ ] Create a repeatable installer build wrapper in `scripts/build-installer.ps1` that resolves the repo root, verifies the published app, locates `ISCC.exe`, and fails fast with actionable errors if prerequisites are missing.
- [ ] Add the Inno Setup definition at `installer/TibiaHuntAnalyzer.iss` with the app display name, version, icon, Start Menu shortcut, optional desktop shortcut, and common uninstall metadata.
- [ ] Compile the installer into `artifacts/windows/installer/TibiaHuntAnalyzer-Setup.exe` while preserving the published app output and avoiding stale artifacts.
- [ ] Validate database safety during installation: do not ship or overwrite a user database, preserve the current configured-path strategy, and avoid destructive migration or cleanup behavior during install or upgrade.
- [ ] Update README or installation documentation to explain the installer workflow, prerequisites, and output folder.
- [ ] Run the installer build script and verify the generated output file and compiler exit code before claiming success.

### Phase 6 audit findings

- The app is already published by `scripts/publish-windows.ps1` into `artifacts/windows/win-x64` and the final executable name is `Tibia Hunt Analyzer.exe`.
- The application uses a per-user SQLite configuration and does not keep the database inside the repository or install folder; the installer must therefore avoid bundling any user database or source database files.
- The current repo does not have Inno Setup installed in the environment (`iscc` is not found on PATH), so the installer implementation must document the external prerequisite and fail clearly when absent.
- The installer should be built on top of the existing app publish output rather than reusing a debug build or development path, to keep deployment behavior consistent with the validated artifact.
- The packaging strategy should remain simple and deterministic: publish first, then compile installer from the published app folder and required resources only.

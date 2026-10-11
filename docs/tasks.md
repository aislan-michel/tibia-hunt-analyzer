# Feature Changes — Implementation Tasks

## Phase 1: Inspect and specify

- [x] Inspect existing projects, dependencies, navigation, settings persistence, database startup, and CSV components.
- [x] Update the product specification for dedicated entity management, database-path settings, return navigation, and the reduced interchange scope.
- [x] Document the proposed database initialization strategy: validate before atomic config persistence; use a single startup-resolved path; activate path changes after restart.
- [x] Record implementation sequence, affected areas, risks, and verification below.
- [x] Obtain approval of this implementation plan before changing application code.

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

## Design notes and risks

- The existing context factory is configured once at startup. The plan avoids runtime factory replacement and requires a restart after a path change; the settings UI must make that explicit.
- Startup currently applies EF migrations before Avalonia is displayed. The implementation must ensure corrupt or incompatible configured databases produce a recoverable, actionable message rather than an opaque crash or silent fallback.
- A candidate new database may be initialized before its path setting is committed. If configuration persistence then fails, the unreferenced new file may remain, but the active database and saved configuration remain unchanged.
- Entity renames preserve historical identity through existing foreign-key references; deactivation remains soft and must never delete referenced entities.
- Removing CSV code must not change the SQLite schema or modify stored session data.

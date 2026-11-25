# Tibia Hunt Analyzer — Feature Changes and Improvements

> Historical implementation request. The requirements below describe the previously approved change set and are retained for traceability; they are not a new or current backlog. Use `Project Specification.md` as the current product target and `tasks.md` for implementation and verification status.

## 1. Role and working methodology

Act as a senior software engineer working on the existing Tibia Hunt Analyzer application.

The application has already been implemented and is working. Your task is to modify the existing codebase according to the requirements below.

Follow Spec-Driven Development (SDD).

Before implementing changes:
1. Inspect the existing project structure and implementation.
2. Read the project's constitution, overview, relevant specifications, technical plans, tasks, and architecture documentation, if available.
3. Identify the affected components, existing behaviors, and dependencies.
4. Update the relevant specifications before changing the implementation.
5. Present a concise implementation plan and identify any risks.

Do not recreate the application from scratch.

Preserve existing functionality unless a requirement explicitly changes or removes it.

Do not implement unrelated improvements or introduce unnecessary dependencies.

## 2. Technology constraints

Respect the technology stack already used by the project.

The expected stack is:
- C#
- .NET 10 LTS
- Avalonia UI
- XAML
- MVVM with CommunityToolkit.Mvvm
- SQLite
- Entity Framework Core
- xUnit

Inspect the existing project files before assuming these technologies or changing package versions.

The application must continue supporting Windows and Linux.

## 3. Requirement 1 — Change the home screen navigation

Modify the home screen to include the following buttons:

1. Insert Hunt Analyzer
2. Query Hunt Analyzer
3. Manage Characters
4. Manage Hunt Locations

Keep the application settings button with its gear icon in the upper-right corner.

The home screen must provide direct access to character and hunt location management.

### Acceptance criteria

- The home screen displays all four navigation buttons.
- Manage Characters opens the character CRUD screen.
- Manage Hunt Locations opens the hunt location CRUD screen.
- Existing insertion and query navigation continues working.
- The settings button continues opening the settings screen.
- Navigation must follow the existing MVVM architecture and conventions.

Use the existing UI style and keep the interface simple, consistent, and accessible.

## 4. Requirement 2 — Move character and hunt location management out of settings

The settings screen must no longer be responsible for registering or managing characters and hunt locations.

Create or reuse dedicated CRUD screens for these entities.

### Character management

Users must be able to:
- List registered characters.
- Create a character.
- Edit a character.
- Deactivate or delete a character according to the existing data integrity strategy.

### Hunt location management

Users must be able to:
- List registered hunt locations.
- Create a hunt location.
- Edit a hunt location.
- Deactivate or delete a hunt location according to the existing data integrity strategy.

### Business rules

- Character and hunt location records must persist in SQLite.
- Existing hunt sessions must not become invalid when a referenced character or hunt location is modified.
- Historical records must be preserved.
- If the existing application uses soft deletion, preserve that behavior.
- Do not introduce destructive deletion of referenced entities.
- Validate required fields and prevent invalid records.
- Follow the existing UI and validation conventions.

### Acceptance criteria

- Neither character management nor hunt location management appears in the settings screen.
- Both entities can be managed through their dedicated home screen navigation buttons.
- Changes persist after restarting the application.
- Existing hunt sessions remain accessible and retain their historical associations.

## 5. Requirement 3 — Configure the database path

The settings screen must be redesigned to manage the SQLite database file path.

Remove character and hunt location management controls from this screen.

Add a field displaying the configured database path and an option to browse for a database location.

The user must be able to:
- View the current database path.
- Select a database file or configure a valid location for a new database.
- Save the selected configuration.
- Receive understandable success or error messages.

Use the existing settings button and preserve the current button ordering where applicable.

### Database path behavior

Inspect the current database initialization and configuration implementation before making changes.

Define and implement a safe strategy for changing the database path.

Requirements:
- Persist the configured path so it survives application restarts.
- Do not silently replace the current database.
- Do not automatically overwrite an existing database.
- Validate that the selected location is usable.
- Handle missing files, invalid paths, inaccessible directories, and database connection failures.
- If a new database is created, initialize its schema through the existing EF Core migration strategy.
- If an existing database is selected, verify that it is a compatible Tibia Hunt Analyzer database before using it.
- Do not run destructive migrations or reset existing data automatically.
- Do not lose unsaved changes or silently discard the current database configuration when a new path is invalid.

If changing the database path requires reconnecting the EF Core DbContext, implement that behavior safely.

Avoid maintaining multiple conflicting database paths in memory.

### Proposed database initialization and reconnection strategy

The current application constructs an EF Core context factory from a fixed path during dependency injection and applies migrations before starting Avalonia. Reconfiguring that factory while existing services or contexts may still be in use would risk mixed reads and writes across two databases.

Proposed approach for approval:
- Store the selected path in a small application configuration file under the stable per-user application data directory, separate from the selected database.
- Resolve the effective path once during startup and use that same resolved value for the context factory and every path-dependent service.
- Validate a candidate before atomically persisting it. A path to a nonexistent file is initialized with the existing EF Core migrations; an existing file must pass SQLite integrity, application-schema, and migration-history checks. Never truncate or reinterpret an existing arbitrary/empty file as a new database.
- Only apply known application migrations after reviewing them as non-destructive; reject corrupt, incompatible, future, or otherwise unsafe schemas before switching.
- Do not hot-swap the database while the application is running. Report that a successfully saved path takes effect after restart; the current process continues using its original factory. This avoids live DbContext reconnection and mixed-database operations.
- Persist the selected path only after the target is usable. A failed validation leaves both the stored configuration and current database unchanged.

This strategy trades immediate switching for simpler and safer lifetime management. Startup failure recovery for an externally removed or later-corrupted configured database must provide a clear error without silently selecting another database.

### Acceptance criteria

- The settings screen displays the active database path.
- A valid path can be configured and persisted.
- Restarting the application uses the saved configuration.
- Invalid paths and incompatible databases produce useful error messages.
- Existing data is preserved.
- A failed path change does not silently leave the application in an inconsistent state.

Before implementing database switching, explain the proposed initialization and reconnection strategy.

## 6. Requirement 4 — Remove CSV import and export

Remove the CSV import and export functionality from the application.

This includes:
- CSV import UI controls.
- CSV export UI controls.
- CSV-specific navigation or menu items.
- CSV import and export application services.
- CSV-specific commands and ViewModels.
- CSV-specific dependency registrations.
- CSV-specific tests that are no longer applicable.
- CSV-specific dependencies, such as CsvHelper, if they are not used elsewhere.
- CSV-related documentation and specifications.

Do not remove JSON file import or text-based Hunt Analyzer input.

Do not remove SQLite persistence.

Do not remove functionality for querying, viewing, or analyzing hunt sessions.

### Data preservation

Removing CSV support must not delete or modify existing hunt session records.

If CSV functionality is isolated in dedicated components, remove those components cleanly.

If any CSV-related code is shared with other features, inspect its dependencies before removing it.

Update the SDD documentation to reflect the removal of CSV functionality and ensure that the application no longer advertises CSV support.

### Acceptance criteria

- There are no CSV import or export actions in the user interface.
- The application no longer exposes CSV import or export use cases.
- Existing JSON and text import workflows continue working.
- Existing sessions and analytics continue working.
- Unused CSV dependencies are removed.
- The solution builds successfully and the relevant tests pass.

## 7. Requirement 5 — Fix the Exit button on the Hunt Analyzer registration screen

Currently, clicking the Exit button on the Insert Hunt Analyzer screen closes the entire application.

This behavior must change.

The Exit button must navigate back to the previous screen instead of terminating the application.

### Expected behavior

When the user opens Insert Hunt Analyzer from the home screen and clicks Exit:
- The registration screen closes or is dismissed.
- The user returns to the previous screen.
- The main application remains running.

Apply the same navigation principle to other screens where an Exit or Back button is intended to return to the previous screen rather than close the entire application.

The actual application shutdown must remain available through the appropriate application-level action.

### Unsaved data

If the user has entered information into the form, follow the existing unsaved-changes behavior.

If no such behavior exists, propose a minimal, consistent approach that prevents accidental data loss without introducing unnecessary complexity.

### Acceptance criteria

- Clicking Exit on the registration screen does not terminate the application.
- The user returns to the appropriate previous screen.
- The form is handled consistently when it contains unsaved data.
- The application's actual shutdown behavior remains correct.
- Existing navigation and registration behavior continue working.

Inspect the current navigation implementation before making changes. Do not simply replace one application-wide shutdown call with another arbitrary navigation call.

## 8. SDD documentation updates

Before modifying application code, update the relevant specifications and plans.

At minimum:
- Update the overview to remove CSV import/export from the product scope.
- Update the settings specification to describe database path management.
- Update the home screen specification to include character and hunt location management navigation.
- Update the character and hunt location management specifications to reflect their dedicated screens.
- Update the navigation specification to distinguish returning to a previous screen from terminating the application.
- Update the architecture documentation if the database initialization or navigation design changes.
- Update tasks.md files with the implementation tasks and verification steps.

Preserve the existing SDD conventions and file organization.

Do not create duplicate specifications if equivalent documents already exist.

Keep documentation consistent with the actual application behavior.

## 9. Testing requirements

Add or update automated tests to cover the changes.

At minimum, verify:

### Navigation
- Home screen navigation opens character management.
- Home screen navigation opens hunt location management.
- Existing insertion and query navigation still works.
- Exiting the registration screen returns to the previous screen without terminating the application.

### CRUD
- Characters can be created, retrieved, updated, and deactivated.
- Hunt locations can be created, retrieved, updated, and deactivated.
- Existing sessions remain associated with their historical character and location records.

### Database path
- A valid database path can be configured.
- The configured path persists across application restarts.
- Invalid paths are handled correctly.
- Incompatible databases are rejected safely.
- Failed database switching does not corrupt existing data or application state.

### CSV removal
- No CSV-specific functionality remains exposed.
- Removing CSV support does not break JSON import, text import, session queries, or analytics.

Use unit tests for business logic and integration tests for database behavior.

Use temporary SQLite databases for integration tests when appropriate.

Do not introduce brittle UI tests if the current testing infrastructure does not support them. If UI automation is unavailable, test navigation commands and their associated behavior at the appropriate architectural level.

## 10. Implementation workflow

Work incrementally.

### Phase 1 — Inspect and specify
- Inspect the repository and the current implementation.
- Identify affected files and dependencies.
- Update the SDD specifications.
- Present the implementation plan.
- Do not modify application code until the plan is clear.

### Phase 2 — Implement
Implement the changes in small, logically grouped steps.

Recommended order:
1. Fix registration screen navigation.
2. Move character and hunt location management to dedicated CRUD screens.
3. Implement database path configuration.
4. Remove CSV functionality.
5. Update remaining documentation and cleanup.

Adjust the order if dependencies in the existing implementation require it, and explain why.

### Phase 3 — Verify
- Build the complete solution.
- Run relevant unit and integration tests.
- Verify JSON and text imports.
- Verify query and analytics workflows.
- Check that existing data remains intact.
- Inspect for unused dependencies and obsolete CSV references.
- Report any tests or scenarios that could not be executed.

Do not claim success for unverified behavior.

### Phase 4 — Report
Provide a concise report containing:
- Specifications updated.
- Files and projects modified.
- Implemented changes.
- Tests executed and their actual results.
- Known limitations or unresolved questions.
- Any manual verification steps still required.

Do not implement additional features beyond the requirements in this request.

Start with Phase 1 and wait for approval before implementing application code.
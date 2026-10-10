# Tibia Hunt Analyzer — Project Specification

## 1. Role and working methodology

Act as a senior software engineer and software architect specializing in C#, .NET, desktop applications, clean architecture, automated testing, and software quality.

Help me develop a desktop application called **Tibia Hunt Analyzer**.

Follow a guided vibe coding approach:
- Work incrementally, delivering one small, functional step at a time.
- Explain architectural decisions and trade-offs before implementing significant changes.
- Prefer simple, maintainable solutions over unnecessary abstractions.
- Do not implement the entire application at once.
- Do not generate large amounts of code before agreeing on the design of the current step.
- When possible, guide me through implementation rather than simply delivering finished code.
- After each milestone, explain how to run and test the implementation.
- Never claim that code was executed or tests passed unless they were actually executed.
- Ask for clarification when a requirement is ambiguous and materially affects the design.

## 2. Project objective

Build a cross-platform desktop application that records, stores, queries, exports, and analyzes Tibia Hunt Analyzer sessions.

The application must work offline and persist its data locally.

Supported desktop platforms:
- Windows
- Linux

The application must be simple, clean, accessible, and easy to maintain.

## 3. Technology stack

Use the following technologies:

- Language: C#
- Runtime: .NET 10 LTS
- Desktop UI: Avalonia UI with XAML
- Architecture: Clean Architecture principles, applied pragmatically
- Presentation pattern: MVVM using CommunityToolkit.Mvvm
- Database: SQLite
- ORM: Entity Framework Core with a compatible SQLite provider
- JSON processing: System.Text.Json
- CSV processing: CsvHelper
- Dependency injection: Microsoft.Extensions.DependencyInjection
- Logging: Microsoft.Extensions.Logging
- Unit testing: xUnit
- Validation: FluentValidation
- Integration testing: xUnit with temporary SQLite databases

Before adding dependencies, verify their compatibility with the selected .NET and Avalonia versions.

Do not introduce a web API, a remote database, cloud services, or unnecessary infrastructure.

## 4. Architecture

Create a solution organized into these projects:

src/
- TibiaHuntAnalyzer.Desktop
- TibiaHuntAnalyzer.Application
- TibiaHuntAnalyzer.Domain
- TibiaHuntAnalyzer.Infrastructure

tests/
- TibiaHuntAnalyzer.Tests.Unit
- TibiaHuntAnalyzer.Tests.Integration

Responsibilities:

### Desktop
Contains Avalonia views, ViewModels, UI resources, navigation, and presentation logic.

### Application
Contains use cases, application services, DTOs, validation, and abstractions for infrastructure dependencies.

### Domain
Contains domain entities, value objects, and business rules. It must not depend on the UI, database, or infrastructure implementations.

### Infrastructure
Contains SQLite persistence, EF Core configurations and migrations, repositories where justified, importers, exporters, and settings persistence.

### Tests.Unit
Covers domain rules, validation, parsers, and analytical calculations.

### Tests.Integration
Covers persistence, queries, and important workflows involving SQLite.

Maintain dependency direction and avoid circular project references.

Do not create unnecessary interfaces, repositories, or abstractions without a clear reason.

## 5. Main application screen

Create a home screen containing:

- A settings button with a gear icon in the upper-right corner.
- A centered button labeled "Inserir Hunt Analyzer".
- A button labeled "Consultar Hunt Analyzer" below the insertion button.

The interface must be responsive to reasonable window sizes, keyboard-accessible, and visually consistent.

Use clear labels, visible focus states, appropriate contrast, and understandable validation messages.

## 6. Settings screen

The settings screen must allow the user to manage:

### Characters
- Add a character.
- Edit a character.
- Deactivate a character without losing historical records.
- Display the registered characters.

### Hunt locations
- Add a hunt location.
- Edit a hunt location.
- Deactivate a hunt location without losing historical records.
- Display the registered locations.

Settings must persist between application sessions.

Place the buttons side by side in this exact order:
1. Sair
2. Salvar

Saving must validate the data and report success or failure.

## 7. Insert Hunt Analyzer screen

The form must contain the following required fields:

1. Character name: dropdown populated from active characters.
2. Character level: positive integer.
3. Hunt name: dropdown populated from active hunt locations.
4. Input format: JSON file or text copied from Tibia.
5. Input content: file selection for JSON or a text area for copied content.

The form must display the buttons side by side in this exact order:
1. Sair
2. Limpar
3. Enviar

Behavior:

- Sair closes the application, subject to any appropriate unsaved-data confirmation.
- Limpar clears the form after handling any unsaved input appropriately.
- Enviar validates the input, parses the session, and persists the complete record.
- On success, display a clear confirmation.
- On failure, display a useful list of validation or processing errors.
- Never report success before persistence completes.
- Never persist a partially parsed session.
- Do not prevent legitimate duplicate sessions.

All required fields must be validated before saving.

## 8. Supported Tibia data formats

### JSON input

Support JSON files containing fields such as:

- Balance
- Damage
- Damage/h
- Healing
- Healing/h
- Killed Monsters
- Loot
- Looted Items
- Raw XP Gain
- Raw XP/h
- Session end
- Session length
- Session start
- Supplies
- XP Gain
- XP/h

Example values may be represented as strings, including numeric values encoded as strings.

The parser must handle empty arrays, missing fields, invalid JSON, invalid numbers, and unexpected input types gracefully.

Do not silently replace invalid data with zero.

### Text input

Support text copied from the Tibia Hunt Analyzer, containing session dates, duration, XP, loot, supplies, balance, damage, healing, killed monsters, and looted items.

Handle empty lists and the "None" representation.

Separate parsing from UI code. Create dedicated parsers that convert each input format into a common internal representation.

Preserve the original input format and, where appropriate, the original content for troubleshooting and auditability.

Do not assume that all fields are always present or that the input formatting will never change.

## 9. Domain model

Design and implement the domain model incrementally.

Consider these entities:

- Character
- HuntLocation
- HuntSession
- HuntStatistics
- KilledMonster
- LootedItem

A HuntSession must reference a character and a hunt location and contain the selected character level, session start and end times, duration, and associated statistics.

Monster and item records must be associated with their respective hunt session.

Store numeric statistics in appropriate numeric types rather than retaining them as strings.

Preserve the distinction between XP Gain and Raw XP Gain, as well as between their hourly values.

Preserve the difference between session duration and elapsed time between session start and end.

Use database relationships and constraints where appropriate.

Choose appropriate decimal or integer types for financial values and counts, and document assumptions about the source format.

Do not invent item values or monster counts that are absent from the input.

Initial domain-model assumptions:
- XP, damage, healing, monster counts, and item quantities use non-negative 64-bit integers when present.
- Hourly rates and currency amounts use decimals; balance may be negative.
- Source metrics and entity counts remain nullable when omitted by the imported data.
- `Duration` is the source-reported hunt duration; elapsed time is separately derived from session start and end.
- Item values are not modeled until the input formats establish whether a value is per-item or a total.
- The initial JSON parser accepts a root object, case-insensitive documented metric keys, record arrays with `Name` plus an optional `Killed`/`Count` or `Amount`/`Quantity`/`Count`, invariant-culture numbers (as strings or JSON numbers), ISO-compatible timestamps and Tibia's `yyyy-MM-dd, HH:mm:ss` timestamp representation (interpreted as UTC when no offset is present), and `TimeSpan`/clock/unit duration strings including `00:00h`. Missing metrics and lists remain null and empty respectively; malformed present values are errors.
- The initial text parser supports the supplied `From yyyy-MM-dd, HH:mm:ss to ...` header, `Session:`/`Session length:` duration, colon-separated metrics, and one monster/item entry per line in `Nx name` form. It interprets timestamps without an offset using the computer's local timezone at import. Numbers use invariant formatting with comma thousands separators; absent values stay null, and `None` or empty list sections produce empty lists.
- Hunt sessions are stored as relational rows with statistics in typed columns and monster/item rows in child tables; the original source format and text are retained. Session writes use one SQLite transaction and do not add uniqueness constraints that would reject duplicate sessions.
- The insertion screen loads only active characters and hunt locations, parses JSON files or pasted text before saving, and reports parser/validation/persistence errors without claiming success before the save completes. Clearing or exiting with unsaved form input requires a second confirmation.
- The session query retrieves lightweight rows from SQLite, supports case-insensitive character/location filtering, inclusive local-calendar date filters in `YYYY-MM-DD` form, and sorting by character, level, hunt location, start, or end. Selecting a row loads its complete details, including all available statistics, record quantities, source format, and original input.

## 10. Persistence and data integrity

SQLite is the primary source of truth.

Requirements:
- Store data in a suitable per-user application data directory.
- Never depend on the application's installation directory being writable.
- Use EF Core migrations.
- Persist settings and historical sessions.
- Preserve historical records when characters or hunt locations are deactivated.
- Allow multiple sessions with identical character, location, and timestamps if the user explicitly submits them.
- Use transactions when saving a session and its related statistics, monsters, and items.
- Prevent incomplete sessions from being committed.
- Provide clear persistence errors.
- Plan for local database backup and restoration.

Do not store all session data exclusively as an opaque JSON document.

The home screen's backup/restore page exports a consistent SQLite backup to a user-selected `.db` or `.sqlite` file. Restore requires explicit confirmation, stages and validates the selected SQLite file and its application schema, applies available EF migrations to the staged copy, and checks referential integrity before changing the live database. Before restoring, the current database is kept as a timestamped `tibia-hunt-analyzer-pre-restore-*.db` file beside the application database. A failed restore attempts to roll back from that safety copy; failures are reported rather than treated as success.

## 11. CSV import and export

Support both CSV import and CSV export.

CSV export must allow users to export session records and their statistics for use in spreadsheet applications.

CSV import must use a documented, application-defined schema.

Requirements:
- Define and document the CSV column names and data types.
- Handle delimiters, quoting, encoding, and decimal formats consistently.
- Validate imported rows.
- Provide understandable errors identifying invalid rows and fields.
- Avoid partial database updates when an import fails validation.
- Preserve duplicate sessions when importing.
- Do not silently overwrite existing records.

Treat CSV import/export as a separate feature from importing the Tibia JSON and text formats.

The implemented interchange schema is UTF-8 CSV with this exact header order:

`RecordType,SessionId,CharacterId,HuntLocationId,CharacterLevel,SessionStart,SessionEnd,DurationTicks,InputFormat,OriginalInput,Balance,Damage,DamagePerHour,Healing,HealingPerHour,Loot,Supplies,XpGain,XpPerHour,RawXpGain,RawXpPerHour,RecordName,RecordCount`

- `RecordType` is `Session`, `Monster`, or `Item`. Each session has one `Session` row, followed by zero or more child rows.
- `SessionId` is a non-empty GUID used only to group rows within the CSV file. It must be unique for each session in the file; child rows must follow their session row. It is not the database identity, so importing the same file again creates duplicates rather than overwriting records.
- Session rows require `CharacterId` and `HuntLocationId` GUIDs that identify currently active settings, positive `CharacterLevel`, `SessionStart` and `SessionEnd` timestamps in ISO 8601 round-trip (`O`) format, non-negative `DurationTicks`, and `InputFormat` (`Json` or `Text`). `OriginalInput` preserves the source content.
- Numeric values use invariant culture, without localized decimal separators. Integer fields are whole numbers; decimal metrics use `.` as the decimal separator. `Balance`, `Damage`, `DamagePerHour`, `Healing`, `HealingPerHour`, `Loot`, `Supplies`, `XpGain`, `XpPerHour`, `RawXpGain`, and `RawXpPerHour` are optional and blank when absent. Balance may be negative; other metrics must be non-negative. `DurationTicks` stores the reported duration, not elapsed time.
- Child rows require `SessionId` and `RecordName`; `RecordCount` is an optional positive whole number. Other session-only columns are blank on child rows.
- CSV quoting, delimiters, embedded quotes, and multiline `OriginalInput` values follow standard CsvHelper CSV handling. Files should be saved as UTF-8. Spreadsheet applications that alter GUIDs, timestamps, or numeric formatting may make a file unsuitable for re-import.
- Import validates all rows and active setting references before writing. Any validation error rejects the complete file without changing the database; a valid import writes every session and its child records in one transaction. Existing sessions are never overwritten.

## 12. Query and details screens

The query screen must display sessions in a table containing at least:

- Character
- Level
- Hunt name
- Session start
- Session end

Provide useful sorting and filtering where appropriate.

Selecting a row must open a details view showing the session metadata, all available statistics, killed monsters, and looted items.

The query must retrieve the complete session information from SQLite.

## 13. Analytics

Implement the following analytical capabilities after recording, querying, and exporting sessions are reliable.

### Profit analysis
- Balance per session.
- Balance per hour.
- Total balance by character.
- Total balance by hunt location.
- Average balance per session.

### XP analysis
- XP Gain per session.
- XP per hour.
- Total XP over a selected period.
- Comparison of XP per hour across hunt locations.
- Historical character level progression.

### Comparisons
Allow users to filter by character, hunt location, and date range.

Display the number of sessions contributing to aggregated metrics.

Distinguish imported game metrics from values calculated by the application.

Define behavior for zero-duration sessions and missing statistics.

Avoid misleading averages and document the calculation rules.

Do not implement a dashboard before the underlying analytical rules are tested.

The calculation layer uses the source-reported hunt `Duration`, not elapsed time between start and end. Per-session balance/hour, XP/hour, and raw XP/hour are calculated as their corresponding total divided by reported duration in hours; a zero-duration session or missing total produces no calculated rate. Imported XP/hour and raw XP/hour remain separate from calculated rates. Aggregated totals and averages include only sessions with that metric present, and each metric reports its contributor count; the overall session count still includes sessions with missing metrics. Hourly rates are averaged per contributing session and are not summed. Date filters are inclusive and use the session start's local calendar date. Level history contains one point per session, ordered chronologically, and preserves repeated sessions and repeated levels.

The analytics application service reads persisted session metadata and statistics from SQLite, applies character and hunt-location filters in the database, and delegates date filtering and metric calculations to the tested calculation layer. Analytics reports do not load source input, monsters, or items because those records do not contribute to the current metrics.

The desktop analytics screen exposes inclusive date filters and active character/location selectors, presents overall and grouped balance/XP metrics, distinguishes game-reported hourly values from calculated rates, and shows the contributing-session count alongside each displayed metric. Character level history is displayed chronologically. The screen is a report view, not a chart dashboard.

## 14. Coding standards

- Write code, identifiers, class names, method names, variables, and technical documentation in English.
- Use Portuguese comments only when they clarify a non-obvious implementation detail.
- Prefer descriptive, meaningful names.
- Keep methods focused and reasonably small.
- Avoid duplicated parsing and validation logic.
- Follow SOLID principles pragmatically.
- Use asynchronous APIs for I/O operations when appropriate.
- Do not block the UI thread during file operations, database operations, or large queries.
- Handle exceptions at appropriate application boundaries.
- Do not expose raw stack traces to end users.
- Never silently swallow exceptions.
- Avoid unnecessary dependencies and premature optimization.

## 15. Testing requirements

Write automated tests for the most important scenarios, including:

- Valid JSON import.
- Valid text import.
- Invalid JSON.
- Missing required fields.
- Invalid numeric values.
- Empty monster and item lists.
- Date and duration handling.
- Failed validation without persistence.
- Successful persistence of a complete session.
- Transaction rollback when persistence fails.
- Retrieval of session details.
- CSV import and export.
- Profit and XP calculations.
- Zero-duration sessions.
- Duplicate session handling.

Use representative fixtures based on the supplied Tibia examples.

Tests must be deterministic and must not depend on a live game client or internet connection.

Do not claim tests pass unless they have actually been executed.

## 16. Development workflow

Work in small, reviewable milestones.

For each milestone:

1. Explain the objective and relevant design decisions.
2. Identify the files and projects that will change.
3. Present the implementation plan before making extensive changes.
4. Implement only the agreed scope.
5. Run the relevant build and tests when tools are available.
6. Report exactly what was implemented and verified.
7. Explain any remaining limitations.
8. Suggest the next milestone without implementing it automatically.

If you can access the project files, inspect the existing solution before modifying it.

Do not overwrite existing work without understanding its purpose.

Do not skip ahead to later phases without confirming the current phase is working.

## 17. First task

Start with Phase 1: project foundation.

Do not implement the complete application yet.

First:
1. Confirm the development environment and available SDK.
2. Explain the required tools for Windows and Linux development.
3. Propose the initial solution and project structure.
4. Verify the compatibility of the selected Avalonia, .NET, and EF Core versions.
5. Guide me through creating the solution and projects.
6. Establish project references and dependency injection conventions.
7. Create a minimal Avalonia application that builds and launches.
8. Add the unit and integration test projects and verify that tests run.

Finish Phase 1 with a working, buildable solution and a short checklist of verified results.

Wait for my confirmation before proceeding to Phase 2.
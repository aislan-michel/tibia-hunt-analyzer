# Project Guidelines

## Project context

- Tibia Hunt Analyzer is a cross-platform Avalonia desktop application using C#/.NET 10, MVVM, SQLite, and EF Core. It operates offline and stores user data locally.
- Treat `docs/Project Specification.md` as the current product target. `docs/Feature Changes and Improvements.md` and `docs/tasks.md` are historical records, not active backlogs.
- Preserve the existing architecture and conventions. Inspect the relevant code and tests before changing behavior; do not recreate functionality or introduce unrelated improvements.

## Working with requirements

- Work incrementally and implement only the scope the user has agreed to.
- Before significant changes, explain the intended behavior, affected areas, and relevant trade-offs. Ask the user when a material product or design decision is ambiguous.
- Do not infer that a requirement is implemented merely because it appears in the specification. Check the current code and tests, and distinguish automated verification from manual checks.
- Keep user-facing messages clear and consistent with the existing UI language.

## Architecture and data safety

- Keep domain rules independent of Avalonia, SQLite, and infrastructure implementations. Follow the existing dependency direction: Desktop uses Application and Infrastructure; Infrastructure implements application-facing services and persistence.
- Preserve local user data. Never silently replace, reset, or fall back from a configured database.
- At startup, create and migrate the database at the configured path only when the file is absent; validate existing files before use. Apply only reviewed, non-destructive migrations.
- Preserve the existing JSON and pasted-text import workflows. Do not reintroduce CSV import/export unless the user explicitly changes the product scope.

## Build, test, and documentation

- Use the existing scripts where appropriate: `scripts/build.ps1` / `scripts/build.sh` and `scripts/test.ps1` / `scripts/test.sh`.
- Run focused tests for the changed behavior, then the relevant build. Report the commands run and their actual results; never claim unexecuted checks passed.
- Keep code and technical documentation in English, consistent with the project specification. When user-facing behavior changes, update both `README.md` and `README.pt-BR.md` when relevant.
- Before adding a dependency, inspect the existing project files and verify compatibility with the current .NET and Avalonia stack.

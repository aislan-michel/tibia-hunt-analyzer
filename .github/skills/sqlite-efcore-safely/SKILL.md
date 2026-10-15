---
name: sqlite-efcore-safely
description: "Use when changing the SQLite schema, EF Core persistence, database-path handling, or backup and restore. Guides safe migrations and tests that protect existing user data."
argument-hint: "Describe the persistence or schema change to make."
user-invocable: true
---

# Safe SQLite and EF Core Changes

Use this workflow for persistence work in Tibia Hunt Analyzer. Follow `.github/instructions/sqlite-efcore.instructions.md` for the standing data-safety rules.

## Procedure

1. Read the relevant product requirements and inspect the affected entity, `HuntAnalyzerDbContext`, infrastructure service, migration history, and integration tests.
2. Identify whether the change affects the schema, database-path configuration, queries/writes, or backup/restore. State the smallest scope and any material data migration risk. Ask the user before choosing between materially different data behaviors.
3. For schema changes, follow the existing EF migration setup and the repository-pinned `dotnet-ef` tool. Generate migrations for `TibiaHuntAnalyzer.Infrastructure`; inspect every generated operation and the model snapshot for unintended data loss or unrelated changes.
4. Apply only reviewed, non-destructive migrations. Update `DatabasePathCompatibility`'s reviewed migration allowlist only after reviewing the migration. Do not use `dotnet ef database update` against a user's database; application startup applies approved migrations to its configured database.
5. For database-path changes, preserve staged initialization, compatibility checks, and atomic configuration persistence. A missing configured file is created at that path only when its parent directory is usable; existing files are validated, never overwritten or silently replaced.
6. For backup/restore changes, preserve staging, integrity and schema validation, a safety backup, rollback behavior, and explicit failure reporting.
7. Add or update focused integration tests using isolated temporary SQLite databases. Cover failure paths and verify that existing database files and configuration remain unchanged when an operation fails.
8. Run the focused integration tests and the relevant build. Update the specification or both READMEs when user-visible behavior changes, and report exactly what was verified.

## Migration command

From the repository root, use the pinned tool and infrastructure project:

```powershell
dotnet tool restore
dotnet ef migrations add NewMigrationName --project .\src\TibiaHuntAnalyzer.Infrastructure\TibiaHuntAnalyzer.Infrastructure.csproj
```

Do not run the command until the user has approved the schema change. Review the generated migration before any application or release uses it.

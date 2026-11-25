---
description: "Use when changing SQLite persistence, EF Core mappings or migrations, database-path configuration, backup/restore, or related integration tests."
name: "SQLite and EF Core"
applyTo:
  - "src/TibiaHuntAnalyzer.Infrastructure/Persistence/**/*.cs"
  - "src/TibiaHuntAnalyzer.Infrastructure/Settings/**/*.cs"
  - "src/TibiaHuntAnalyzer.Infrastructure/Backups/**/*.cs"
  - "tests/TibiaHuntAnalyzer.Tests.Integration/**/*.cs"
---

# SQLite and EF Core

- Follow the existing `HuntAnalyzerDbContext`, `AddDbContextFactory`, and infrastructure-service patterns. Resolve the database path once at startup; do not hot-swap the context factory or let path-dependent services use different databases.
- Use asynchronous EF Core and SQLite APIs for I/O, and dispose contexts, connections, commands, and streams with `using`/`await using` as appropriate.
- Preserve user data. Never truncate, replace, or silently fall back from a configured database. At startup, initialize a missing database at the configured path only when its parent directory is usable; validate an existing file before applying migrations.
- When configuring a new path, initialize a genuinely absent file through the existing staged migration flow and persist the setting only after validation succeeds. Reject existing empty, incompatible, corrupt, or newer-schema databases without modifying them.
- Review every schema migration for data preservation and non-destructive behavior. Update the reviewed-migration allowlist in `DatabasePathCompatibility` only after the migration has been inspected and approved.
- Preserve historical character/location associations and session data. Keep related session writes transactional and follow the existing foreign-key delete behaviors.
- For backup/restore changes, retain staging, validation, integrity checks, a safety backup, and explicit failure reporting. Do not report restore success until the replacement completes.
- Test persistence behavior with isolated temporary SQLite databases, following the integration-test patterns. Cover both successful changes and failure cases that must leave the existing database or saved configuration unchanged.

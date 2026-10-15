# Tibia Hunt Analyzer

[Português (Brasil)](README.pt-BR.md)

Tibia Hunt Analyzer is a desktop application for recording, storing, querying, and analyzing Tibia hunt sessions locally on the user's machine. The application is designed for offline use and keeps the data in a local SQLite database so the user can review historical sessions, session details, and aggregate metrics without relying on external services.

## Project status

This repository contains a working Avalonia-based desktop application with database persistence, settings management, hunt import workflows, session queries, analytics, and Windows publishing and installer workflows. See `docs/tasks.md` for recorded implementation and verification milestones; interactive visual smoke testing on Windows and Linux remains unverified.

## Application version

Current version: 1.0.0

## Features

- Import Hunt Analyzer data from JSON files.
- Import Hunt Analyzer content pasted as text.
- Register and manage characters and hunt locations.
- Record hunt sessions with associated character and location data.
- Query historical sessions and inspect detailed session information.
- View analytics based on session data.
- Manage the local database path in the application settings.
- Store data locally in SQLite and preserve historical records.
- Publish a self-contained Windows executable for the desktop app.

The current implementation does not expose CSV import/export functionality.

## Screenshots

No screenshots are currently included in the repository. If the project adds visual assets in the future, they should be added as relative paths with descriptive alt text and kept small and relevant.

## Technology stack

- C# and .NET 10
- Avalonia UI and XAML
- MVVM with CommunityToolkit.Mvvm
- SQLite with Entity Framework Core
- System.Text.Json for JSON parsing and serialization
- Dependency Injection from Microsoft.Extensions.DependencyInjection
- xUnit for automated testing
- PowerShell scripts for build and publishing workflows

## Architecture and project structure

```text
TibiaHuntAnalyzer/
├─ README.md
├─ README.pt-BR.md
├─ TibiaHuntAnalyzer.slnx
├─ .gitignore
├─ scripts/
│  ├─ build.ps1
│  ├─ build.sh
│  ├─ run.ps1
│  ├─ run.sh
│  ├─ test.ps1
│  ├─ test.sh
│  ├─ publish-windows.ps1
│  └─ build-installer.ps1
├─ installer/
│  └─ TibiaHuntAnalyzer.iss
├─ src/
│  ├─ TibiaHuntAnalyzer.Desktop/
│  │  ├─ ViewModels/
│  │  ├─ Views/
│  │  ├─ App.axaml
│  │  ├─ Program.cs
│  │  └─ TibiaHuntAnalyzer.Desktop.csproj
│  ├─ TibiaHuntAnalyzer.Application/
│  ├─ TibiaHuntAnalyzer.Domain/
│  └─ TibiaHuntAnalyzer.Infrastructure/
├─ tests/
│  ├─ TibiaHuntAnalyzer.Tests.Unit/
│  └─ TibiaHuntAnalyzer.Tests.Integration/
├─ docs/
│  ├─ tasks.md
│  ├─ Project Specification.md
│  └─ Feature Changes and Improvements.md
├─ LICENSE
├─ artifacts/
│  └─ windows/
└─ dotnet-tools.json
```

The application is split into the standard project layers used by the repo:

- `src/TibiaHuntAnalyzer.Desktop` contains the Avalonia UI, views, and screen-level ViewModels.
- `src/TibiaHuntAnalyzer.Application` contains application services, DTOs, imports, analytics, settings, and validation logic.
- `src/TibiaHuntAnalyzer.Domain` contains the core domain entities and business rules.
- `src/TibiaHuntAnalyzer.Infrastructure` contains SQLite persistence, EF Core configuration, database initialization, and infrastructure services.
- `tests/` contains the automated unit and integration tests.
- `docs/` contains the project specification and task tracking documents.
- `scripts/` contains the development and publishing automation.

## Prerequisites

### Development environment

- .NET 10 SDK
- PowerShell 5.1+ or PowerShell 7+
- A Windows machine for the official publish workflow

### End-user runtime

The published Windows application is self-contained and does not require a system-wide .NET SDK installation on the target machine, but the published output still needs to be launched from its generated output directory.

## Installation and development setup

1. Clone the repository.
2. Open a PowerShell session in the repository root.
3. Restore dependencies:

```powershell
dotnet restore .\TibiaHuntAnalyzer.slnx
```

4. Build the solution:

```powershell
./scripts/build.ps1
```

5. Run the application:

```powershell
./scripts/run.ps1
```

6. Run the automated tests:

```powershell
./scripts/test.ps1
```

The repository also includes shell scripts in `scripts/` for Linux-like environments when applicable, but the publish flow is documented as a Windows PowerShell workflow.

## Windows executable publishing

The project includes a publish script at [`scripts/publish-windows.ps1`](scripts/publish-windows.ps1).

Run it from a Windows PowerShell session:

```powershell
./scripts/publish-windows.ps1
```

The script publishes the desktop project in Release configuration for `win-x64`, produces a self-contained bundle, and writes the output to:

```text
artifacts/windows/win-x64/
```

The generated executable is named:

```text
artifacts/windows/win-x64/Tibia Hunt Analyzer.exe
```

The publish process is intentionally configured to keep the produced output as a standard Windows application bundle (`PublishSingleFile=false`), validates that the `.exe` was created successfully, and renames the final output to the user-facing product name without breaking the dependency files that accompany it.

## Windows installer build

The repository also includes an installer build script at [`scripts/build-installer.ps1`](scripts/build-installer.ps1) and the Inno Setup definition at [`installer/TibiaHuntAnalyzer.iss`](installer/TibiaHuntAnalyzer.iss).

Run the installer workflow from a Windows PowerShell session:

```powershell
./scripts/build-installer.ps1
```

The script:

1. Resolves the repository root reliably.
2. Verifies the existing Windows app publish output.
3. Validates the published `Tibia Hunt Analyzer.exe` exists.
4. Locates `ISCC.exe` using the standard Inno Setup installation paths or PATH.
5. Stops with a clear error if Inno Setup is not installed.
6. Compiles the installer to:

```text
artifacts/windows/installer/TibiaHuntAnalyzer-Setup.exe
```

The installer is designed for a per-user installation model, creates a Start Menu shortcut, offers an optional desktop shortcut, and installs the published application files without altering the user database configuration or repository assets.

## Data storage and database configuration

The default SQLite database is stored at `%LOCALAPPDATA%\TibiaHuntAnalyzer\tibia-hunt-analyzer.db` on Windows. On other platforms, the equivalent application-specific directory is based on the operating system's `LocalApplicationData` folder. The separate `database-path.json` configuration file stores a user-selected database path.

At startup, if the configured database file is missing, the app creates and migrates it at that path, provided the parent directory exists and is writable. If a file already exists, the app validates compatibility and does not overwrite it. The application does not silently fall back to another path.

This design preserves user data across application restarts and avoids writing into protected install directories. If the configured location is invalid, inaccessible, or contains an incompatible database, the app reports the issue rather than replacing the database or silently choosing another location.

## Usage guide

A typical workflow is:

1. Launch the app.
2. Open the settings screen to review or change the active database path.
3. Manage characters and hunt locations.
4. Open the insert form and either select a JSON file or paste the text content exported by Tibia Hunt Analyzer.
5. Submit the session after validation.
6. Use the query screen to review previous sessions.
7. Open a session details view to inspect stats, killed monsters, and looted items.
8. Use analytics to review session summaries and derived metrics.

The app is intended to operate offline; there is no remote database dependency in the current implementation.

## Testing and quality

The repository includes both unit and integration tests. The test scripts are configured to run the project solution and the automated tests in the repo.

### Verified results

The following results were verified in the current environment:

- Build: succeeded
- Unit tests: 80 passed, 0 failed
- Integration tests: 17 passed, 0 failed
- Total: 97 passed, 0 failed

The project also includes build and test scripts in [`scripts/`](scripts) for consistent local runs.

## Troubleshooting

### .NET SDK not found

Install the .NET 10 SDK and verify that `dotnet` is available on `PATH` before building or publishing.

### Database path or configuration issues

If the configured database path is invalid, inaccessible, or incompatible, the app will show a clear startup or settings error instead of silently continuing with an unsafe fallback.

### Publish output missing or failed

Use a Windows machine with the .NET 10 SDK installed and run the publish script from the repository root. The script fails with an error if the project is missing or the expected executable is not generated.

### Running the app from source

The desktop app is intended to be launched from the source project during development or from the published output directory for end-user execution. The generated publish folder is the expected runtime artifact for a Windows deployment.

## Contributing and development conventions

This repository keeps specification and task documentation in [`docs/`](docs). The current workflow emphasizes reading the specification before implementation, updating the relevant documentation when necessary, and validating behavior with targeted tests before finalizing changes.

Relevant files:

- [docs/Project Specification.md](docs/Project Specification.md)
- [docs/tasks.md](docs/tasks.md)
- [docs/Feature Changes and Improvements.md](docs/Feature Changes and Improvements.md)

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for the full text.

## Acknowledgements

This project uses the current .NET, Avalonia, EF Core, and xUnit ecosystem components defined in the repository project files. No additional third-party attribution beyond those package dependencies was identified in the repository documentation.

# Tibia Hunt Analyzer

Tibia Hunt Analyzer is a desktop application for recording, storing, querying, and analyzing Tibia hunt sessions locally on the user's machine.

## Requirements for building

- Windows 10 or later for the Windows publish workflow
- .NET 10 SDK
- PowerShell 5.1+ or PowerShell 7+

## Build and test

From the repository root:

```powershell
./scripts/build.ps1
./scripts/test.ps1
```

The repository currently includes Linux shell scripts in `scripts/` for local build/test workflows, and the Windows publishing script is added as a PowerShell equivalent.

## Windows publish

Run the following from a Windows PowerShell session:

```powershell
./scripts/publish-windows.ps1
```

This publishes the desktop app in Release configuration for Windows x64 to:

```text
artifacts/windows/win-x64/
```

The output is self-contained and includes the .NET runtime required to run the application without a separate local .NET installation on the target machine. The published app is not forced into single-file packaging; it is produced as a standard Windows application bundle with the `.exe` and the required support files kept beside it.

To launch the app, open the generated executable directly from the publish directory:

```text
artifacts/windows/win-x64/TibiaHuntAnalyzer.Desktop.exe
```

## Database and user data

The application stores its SQLite database and configuration under the per-user application data directory (`LocalApplicationData\TibiaHuntAnalyzer`) rather than the install directory. This keeps user data writable and avoids overwriting the existing database during normal startup or publication.

## Known limitations

- The Windows publish script is intended for a Windows build machine; it is not meant to be used as a Linux/macOS desktop runtime workflow.
- The publish process validates the presence of the executable and returns a nonzero exit code if publishing fails or the output is missing.
- The application should be opened directly from the generated output folder; it is not intended to be launched through the source repository or a development host.

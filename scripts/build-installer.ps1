$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptDir '..')).Path
$publishScriptPath = Join-Path $repoRoot 'scripts\publish-windows.ps1'
$projectPath = Join-Path $repoRoot 'src\TibiaHuntAnalyzer.Desktop\TibiaHuntAnalyzer.Desktop.csproj'
$publishedOutputRoot = Join-Path $repoRoot 'artifacts\windows\win-x64'
$installerRoot = Join-Path $repoRoot 'installer'
$installerScriptPath = Join-Path $installerRoot 'TibiaHuntAnalyzer.iss'
$installerOutputRoot = Join-Path $repoRoot 'artifacts\windows\installer'
$generatedInstallerPath = Join-Path $installerRoot 'TibiaHuntAnalyzer.generated.iss'
$finalInstallerName = 'TibiaHuntAnalyzer-Setup.exe'
$finalExecutableName = 'Tibia Hunt Analyzer.exe'
$finalExecutablePath = Join-Path $publishedOutputRoot $finalExecutableName
$iconPath = Join-Path $repoRoot 'src\TibiaHuntAnalyzer.Desktop\Assets\tibia-hunt-analyzer-icon.ico'

function Assert-FileExists {
    param(
        [string]$Path,
        [string]$Description
    )

    if (-not (Test-Path -Path $Path)) {
        throw "$Description was not found: $Path"
    }
}

function Find-Iscc {
    $command = Get-Command iscc -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $candidates = @(
        'D:\Inno Setup 6\ISCC.exe',
        'D:\Program Files\Inno Setup 6\ISCC.exe',
        'D:\Program Files (x86)\Inno Setup 6\ISCC.exe',
        'C:\Program Files\Inno Setup 6\ISCC.exe',
        'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
        'C:\Program Files\Inno Setup 5\ISCC.exe',
        'C:\Program Files (x86)\Inno Setup 5\ISCC.exe'
    )

    foreach ($candidate in $candidates) {
        if (Test-Path -Path $candidate) {
            return $candidate
        }
    }

    return $null
}

Assert-FileExists -Path $publishScriptPath -Description 'Windows publish script'
Assert-FileExists -Path $projectPath -Description 'Desktop project file'
Assert-FileExists -Path $installerScriptPath -Description 'Inno Setup script'
Assert-FileExists -Path $iconPath -Description 'Installer icon file'

Write-Host 'Ensuring the published Windows app exists before building the installer...'
if (-not (Test-Path -Path $publishedOutputRoot)) {
    Write-Host 'Published app output not found; running the Windows publish script...'
    & $publishScriptPath
    if ($LASTEXITCODE -ne 0) {
        throw "The Windows publish script failed with exit code $LASTEXITCODE."
    }
}

Assert-FileExists -Path $finalExecutablePath -Description 'Published app executable'

$filesInOutput = @(Get-ChildItem -Path $publishedOutputRoot -File)
if ($filesInOutput.Count -eq 0) {
    throw "The published app output directory is empty: $publishedOutputRoot"
}

$isccPath = Find-Iscc
if (-not $isccPath) {
    throw "Inno Setup was not found. Install Inno Setup 6 or add ISCC.exe to PATH, then rerun this script. Download: https://jrsoftware.org/isinfo.php"
}

if (Test-Path -Path $installerOutputRoot) {
    Write-Host "Cleaning installer output folder at $installerOutputRoot"
    Remove-Item -Path $installerOutputRoot -Recurse -Force
}

New-Item -Path $installerOutputRoot -ItemType Directory -Force | Out-Null

$generatedContent = Get-Content -Path $installerScriptPath -Raw
$generatedContent = $generatedContent.Replace('OutputDir=..\artifacts\windows\installer', "OutputDir=$installerOutputRoot")
$generatedContent = $generatedContent.Replace('OutputDir=.', "OutputDir=$installerOutputRoot")
$generatedContent = $generatedContent.Replace('SourceDir=..\artifacts\windows\win-x64', "SourceDir=$publishedOutputRoot")
$generatedContent = $generatedContent.Replace('SourceDir=.', "SourceDir=$publishedOutputRoot")
$generatedContent = $generatedContent.Replace('SetupIconFile=..\src\TibiaHuntAnalyzer.Desktop\Assets\tibia-hunt-analyzer-icon.ico', "SetupIconFile=$iconPath")
$generatedContent = $generatedContent.Replace('SetupIconFile=.', "SetupIconFile=$iconPath")
$generatedContent = $generatedContent.Replace('LicenseFile=..\LICENSE', "LicenseFile=$repoRoot\LICENSE")
Set-Content -Path $generatedInstallerPath -Value $generatedContent -Encoding UTF8

Write-Host "Compiling installer using $isccPath"
& $isccPath $generatedInstallerPath
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup compilation failed with exit code $LASTEXITCODE."
}

$finalInstallerPath = Join-Path $installerOutputRoot $finalInstallerName
if (-not (Test-Path -Path $finalInstallerPath)) {
    throw "The installer was not generated at $finalInstallerPath"
}

if (Test-Path -Path $generatedInstallerPath) {
    Remove-Item -Path $generatedInstallerPath -Force
}

Write-Host "Installer generated successfully: $finalInstallerPath"
exit 0

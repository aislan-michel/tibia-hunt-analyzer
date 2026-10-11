$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptDir '..')).Path
$projectPath = Join-Path $repoRoot 'src\TibiaHuntAnalyzer.Desktop\TibiaHuntAnalyzer.Desktop.csproj'
$configuration = 'Release'
$runtimeIdentifier = 'win-x64'
$outputRoot = Join-Path $repoRoot 'artifacts\windows\win-x64'
$projectName = [System.IO.Path]::GetFileNameWithoutExtension($projectPath)
$exePath = Join-Path $outputRoot "$projectName.exe"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error 'The .NET SDK is not available on PATH. Install the .NET 10 SDK before publishing.'
    exit 1
}

if (-not (Test-Path $projectPath)) {
    Write-Error "The desktop project was not found: $projectPath"
    exit 1
}

if (Test-Path $outputRoot) {
    Write-Host "Cleaning existing publish output at $outputRoot"
    Remove-Item -Path $outputRoot -Recurse -Force
}

Write-Host "Publishing $projectName for $runtimeIdentifier in $configuration configuration..."
& dotnet publish `
    $projectPath `
    --configuration $configuration `
    --runtime $runtimeIdentifier `
    --self-contained true `
    --output $outputRoot `
    --nologo `
    -p:UseAppHost=true `
    -p:PublishSingleFile=false `
    -p:PublishTrimmed=false

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed with exit code $LASTEXITCODE."
    exit $LASTEXITCODE
}

if (-not (Test-Path $exePath)) {
    Write-Error "The expected Windows executable was not found after publishing: $exePath"
    exit 1
}

Write-Host "Publish succeeded. Output: $outputRoot"
Write-Host "Executable: $exePath"
exit 0

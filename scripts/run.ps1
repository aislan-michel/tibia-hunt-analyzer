$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptDir '..')).Path
$projectPath = Join-Path $repoRoot 'src\TibiaHuntAnalyzer.Desktop\TibiaHuntAnalyzer.Desktop.csproj'

if (-not (Test-Path $projectPath)) {
    Write-Error "The desktop project was not found: $projectPath"
    exit 1
}

& dotnet run --project $projectPath @args
exit $LASTEXITCODE

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptDir '..')).Path

& dotnet build (Join-Path $repoRoot 'TibiaHuntAnalyzer.slnx') @args
exit $LASTEXITCODE

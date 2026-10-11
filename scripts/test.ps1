$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptDir '..')).Path

& dotnet test (Join-Path $repoRoot 'TibiaHuntAnalyzer.slnx') @args
exit $LASTEXITCODE

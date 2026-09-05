param([switch]$Worker)
$ErrorActionPreference = 'Stop'
$pluginRoot = Split-Path -Parent $PSScriptRoot
$state = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'CodexTokenQuest'
New-Item -ItemType Directory -Path $state -Force | Out-Null
if (!$Worker) {
    $shell = (Get-Process -Id $PID).Path
    Start-Process -FilePath $shell -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-WindowStyle', 'Hidden', '-File', ('"' + $PSCommandPath + '"'), '-Worker') -WindowStyle Hidden
    exit 0
}
# Protect bootstrap compilation only; desktop launch decisions are shared C#.
try { $lease = [IO.File]::Open((Join-Path $state 'bootstrap.lock'), 'OpenOrCreate', 'ReadWrite', 'None') }
catch [IO.IOException] { exit 0 }
try {
    $candidates = @()
    if ($env:DOTNET_ROOT) { $candidates += Join-Path $env:DOTNET_ROOT 'dotnet.exe' }
    $candidates += Join-Path $env:USERPROFILE '.dotnet\dotnet.exe'
    $command = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($command) { $candidates += $command.Source }
    $sdk = $null
    foreach ($candidate in $candidates) {
        if ((Test-Path -LiteralPath $candidate) -and ((& $candidate --list-sdks) -match '^10\.0\.\d+ \[')) { $sdk = $candidate; break }
    }
    if (!$sdk) { throw '.NET 10 SDK is required. Install it from https://dotnet.microsoft.com/download/dotnet/10.0' }
    $env:DOTNET_ROOT = Split-Path -Parent $sdk
    $env:DOTNET_HOST_PATH = $sdk
    Set-Location -LiteralPath $pluginRoot
    & $sdk run --project (Join-Path $pluginRoot 'src\CodexTokenQuest.Launcher') --artifacts-path (Join-Path $state 'build\launcher') -c Release -p:UseSharedCompilation=false -- $pluginRoot *>> (Join-Path $state 'bootstrap.log')
    exit $LASTEXITCODE
}
catch { Add-Content -LiteralPath (Join-Path $state 'bootstrap.log') -Value "$(Get-Date -Format o) $_"; exit 1 }
finally { $lease.Dispose() }

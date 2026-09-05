# Run inside Windows. An isolated copy avoids overwriting Mac build outputs.
param([Parameter(Mandatory=$true)][string]$Source)
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$taskRoot = Join-Path $env:TEMP 'CodexTokenQuest-mac-support-validation'
New-Item -ItemType Directory -Path $taskRoot -Force | Out-Null
& robocopy $Source $taskRoot /E /XD .git bin obj artifacts /NFL /NDL /NJH /NJS | Out-Null
if ($LASTEXITCODE -ge 8) { throw "Source copy failed: $LASTEXITCODE" }
Set-Location -LiteralPath $taskRoot
& dotnet build CodexTokenQuest.slnx -c Release -p:UseSharedCompilation=false -m:1 -nr:false
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$env:DOTNET_HOST_PATH = (Get-Command dotnet).Source
& dotnet tests\CodexTokenQuest.Tests\bin\Release\net10.0\CodexTokenQuest.Tests.dll --render
exit $LASTEXITCODE

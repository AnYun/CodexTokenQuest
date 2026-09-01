param([switch]$Disable)

$pluginRoot = Split-Path -Parent $PSScriptRoot
$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$valueName = 'CodexUsageCompanion'

# Remove startup mechanisms used by older releases. Lifecycle is now owned by Codex hooks.
Remove-ItemProperty -LiteralPath $runKey -Name $valueName -ErrorAction SilentlyContinue
Unregister-ScheduledTask -TaskName 'CodexUsageCompanion-Recovery' -Confirm:$false -ErrorAction SilentlyContinue
Get-Process -Name 'CodexUsageCompanion.Watchdog' -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process -Name 'CodexUsageCompanion.Desktop' -ErrorAction SilentlyContinue | Stop-Process -Force

if ($Disable) {
    Get-Process -Name 'CodexTokenQuest.Desktop' -ErrorAction SilentlyContinue | Stop-Process -Force
    Write-Output 'Codex Token Quest lifecycle launch disabled for the current run.'
    exit 0
}

& (Join-Path $pluginRoot 'scripts\start-desktop.ps1')
Write-Output 'Legacy Codex Usage Companion startup removed. Codex Token Quest SessionStart hook is enabled.'

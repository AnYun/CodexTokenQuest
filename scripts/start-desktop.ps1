$pluginRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $pluginRoot 'src\CodexTokenQuest.Desktop\CodexTokenQuest.Desktop.csproj'
$assemblyPath = Join-Path $pluginRoot 'src\CodexTokenQuest.Desktop\bin\Release\net8.0-windows\CodexTokenQuest.Desktop.exe'
$stateDirectory = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)) 'CodexTokenQuest'
$logPath = Join-Path $stateDirectory 'lifecycle.log'

function Write-LifecycleLog([string]$message) {
    try {
        New-Item -ItemType Directory -Path $stateDirectory -Force -ErrorAction Stop | Out-Null
        Add-Content -LiteralPath $logPath -Value "$(Get-Date -Format o) $message" -ErrorAction Stop
    }
    catch {
        # Lifecycle logging must never prevent the HUD from starting.
    }
}

Write-LifecycleLog "Launch requested. WorkingDirectory=$((Get-Location).Path) PluginRoot=$pluginRoot"

$running = Get-Process -Name 'CodexTokenQuest.Desktop' -ErrorAction SilentlyContinue | Select-Object -First 1
if ($running) {
    Write-LifecycleLog "HUD already running. Pid=$($running.Id)"
    exit 0
}

$needsBuild = !(Test-Path -LiteralPath $assemblyPath)
if (!$needsBuild) {
    $assemblyTimestamp = (Get-Item -LiteralPath $assemblyPath).LastWriteTimeUtc
    $needsBuild = Get-ChildItem -LiteralPath (Join-Path $pluginRoot 'src') -Filter '*.cs' -Recurse |
        Where-Object { $_.LastWriteTimeUtc -gt $assemblyTimestamp } |
        Select-Object -First 1
}

if ($needsBuild) {
    $buildOutput = & dotnet build $projectPath --configuration Release --nologo --verbosity quiet 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-LifecycleLog "Build failed. ExitCode=$LASTEXITCODE"
        [Console]::Error.WriteLine(($buildOutput -join [Environment]::NewLine))
        exit $LASTEXITCODE
    }
}

$process = Start-Process -FilePath $assemblyPath -WorkingDirectory $pluginRoot -PassThru
Write-LifecycleLog "HUD started. Pid=$($process.Id) Executable=$assemblyPath"

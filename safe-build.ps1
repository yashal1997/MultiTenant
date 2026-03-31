param(
    [int]$Port = 5099,
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

function Stop-ProcessSafe {
    param([int]$Id)
    try {
        Stop-Process -Id $Id -Force -ErrorAction Stop
        Write-Host "Stopped process PID $Id"
    }
    catch {
        Write-Host "Could not stop PID $Id (already exited or access denied)."
    }
}

Write-Host "=== Safe build started ==="
Write-Host "Project: $PSScriptRoot"
Write-Host "Configuration: $Configuration"
Write-Host "Port: $Port"

# 1) Stop any process listening on target API port
$listeners = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue |
    Select-Object -ExpandProperty OwningProcess -Unique

if ($listeners) {
    Write-Host "Stopping processes listening on port $Port..."
    foreach ($pid in $listeners) { Stop-ProcessSafe -Id $pid }
}
else {
    Write-Host "No listeners found on port $Port."
}

# 2) Stop project-related processes by command line/path match
$projectPath = [Regex]::Escape($PSScriptRoot)
$projectProcesses = Get-CimInstance Win32_Process |
    Where-Object {
        $_.Name -eq "MultiTenant.Api.exe" -or
        ($_.Name -eq "dotnet.exe" -and $_.CommandLine -match $projectPath)
    } |
    Select-Object -ExpandProperty ProcessId -Unique

if ($projectProcesses) {
    Write-Host "Stopping project-related processes..."
    foreach ($pid in $projectProcesses) { Stop-ProcessSafe -Id $pid }
}
else {
    Write-Host "No project-related processes found."
}

Set-Location $PSScriptRoot

Write-Host "Running dotnet clean..."
dotnet clean -c $Configuration
if ($LASTEXITCODE -ne 0) {
    throw "dotnet clean failed with exit code $LASTEXITCODE"
}

Write-Host "Running dotnet build..."
dotnet build -c $Configuration
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed with exit code $LASTEXITCODE"
}

Write-Host "=== Safe build completed successfully ==="


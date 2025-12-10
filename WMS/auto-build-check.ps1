# Auto Build Check Script
# This script monitors file changes and runs build check automatically

param(
    [string]$WatchPath = "D:\Visual_Studio\WMS\WMS",
    [string[]]$FileExtensions = @("*.cs", "*.cshtml", "*.js", "*.css")
)

Write-Host "🔍 Starting Auto Build Check Monitor..." -ForegroundColor Cyan
Write-Host "Watching: $WatchPath" -ForegroundColor Yellow
Write-Host "File types: $($FileExtensions -join ', ')" -ForegroundColor Yellow
Write-Host "Press Ctrl+C to stop monitoring" -ForegroundColor Yellow
Write-Host "=================================" -ForegroundColor Cyan

# Function to run build check
function Invoke-BuildCheck {
    Write-Host "`n🔄 File changed, running build check..." -ForegroundColor Magenta
    & ".\build-check.ps1"
    Write-Host "`n🔍 Continuing to monitor for changes..." -ForegroundColor Cyan
}

# Set up file watcher
$watcher = New-Object System.IO.FileSystemWatcher
$watcher.Path = $WatchPath
$watcher.IncludeSubdirectories = $true
$watcher.EnableRaisingEvents = $true

# Register event handler
Register-ObjectEvent -InputObject $watcher -EventName "Changed" -Action {
    $path = $Event.SourceEventArgs.FullPath
    $name = $Event.SourceEventArgs.Name
    $changeType = $Event.SourceEventArgs.ChangeType
    
    # Check if file extension matches
    $extensions = @("*.cs", "*.cshtml", "*.js", "*.css")
    $shouldCheck = $false
    
    foreach ($ext in $extensions) {
        if ($name -like $ext) {
            $shouldCheck = $true
            break
        }
    }
    
    if ($shouldCheck) {
        Write-Host "📁 File changed: $name ($changeType)" -ForegroundColor Green
        # Run build check in a separate job to avoid blocking
        Start-Job -ScriptBlock { & ".\build-check.ps1" } | Out-Null
    }
}

try {
    # Keep script running
    while ($true) {
        Start-Sleep -Seconds 1
    }
} finally {
    # Cleanup
    $watcher.Dispose()
    Get-Job | Remove-Job -Force
    Write-Host "`n🛑 Monitoring stopped." -ForegroundColor Red
}

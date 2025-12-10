# Build Check Script for WMS Project
# Run this script after making changes to check for compilation errors

Write-Host "🔨 Checking WMS Project Build..." -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan

# Change to project directory
Set-Location "D:\Visual_Studio\WMS\WMS"

# Run dotnet build
Write-Host "Running dotnet build..." -ForegroundColor Yellow
$buildResult = dotnet build 2>&1

# Check exit code
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ BUILD SUCCESS!" -ForegroundColor Green
    Write-Host "No compilation errors found." -ForegroundColor Green
    
    # Count warnings
    $warningCount = ($buildResult | Select-String "warning").Count
    if ($warningCount -gt 0) {
        Write-Host "⚠️  Found $warningCount warning(s) (non-critical)" -ForegroundColor Yellow
    }
} else {
    Write-Host "❌ BUILD FAILED!" -ForegroundColor Red
    Write-Host "Compilation errors found:" -ForegroundColor Red
    Write-Host $buildResult -ForegroundColor Red
}

Write-Host "=================================" -ForegroundColor Cyan
Write-Host "Build check completed." -ForegroundColor Cyan

# Simple Build Check with Confirmation
# This script asks for confirmation before running dotnet build

Write-Host "`n🔨 Build Check Request" -ForegroundColor Cyan
Write-Host "=====================" -ForegroundColor Cyan

$confirmation = Read-Host "Do you want to run 'dotnet build' to check for compilation errors? (y/N)"

if ($confirmation -eq 'y' -or $confirmation -eq 'Y' -or $confirmation -eq 'yes' -or $confirmation -eq 'Yes') {
    Write-Host "`n🔄 Running dotnet build..." -ForegroundColor Yellow
    
    # Change to project directory
    Set-Location "D:\Visual_Studio\WMS\WMS"
    
    # Run dotnet build
    $buildResult = dotnet build 2>&1
    
    # Check exit code
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n✅ BUILD SUCCESS!" -ForegroundColor Green
        Write-Host "No compilation errors found." -ForegroundColor Green
        
        # Count warnings
        $warningCount = ($buildResult | Select-String "warning").Count
        if ($warningCount -gt 0) {
            Write-Host "⚠️  Found $warningCount warning(s) (non-critical)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "`n❌ BUILD FAILED!" -ForegroundColor Red
        Write-Host "Compilation errors found:" -ForegroundColor Red
        Write-Host $buildResult -ForegroundColor Red
    }
} else {
    Write-Host "`n⏭️  Build check skipped." -ForegroundColor Yellow
}

Write-Host "`n=====================" -ForegroundColor Cyan

# Build Helper Functions
# Helper functions for interactive build checking

function Request-BuildCheck {
    param(
        [string]$Context = "after file changes"
    )
    
    Write-Host "`n🔨 Build Check Request" -ForegroundColor Cyan
    Write-Host "=====================" -ForegroundColor Cyan
    Write-Host "Context: $Context" -ForegroundColor Yellow
    Write-Host ""
    
    $confirmation = Read-Host "Do you want to run 'dotnet build' to check for compilation errors? (y/N)"
    
    if ($confirmation -eq 'y' -or $confirmation -eq 'Y' -or $confirmation -eq 'yes' -or $confirmation -eq 'Yes') {
        return $true
    } else {
        Write-Host "⏭️  Build check skipped by user." -ForegroundColor Yellow
        return $false
    }
}

function Invoke-DotNetBuild {
    Write-Host "`n🔄 Running dotnet build..." -ForegroundColor Yellow
    Write-Host "=========================" -ForegroundColor Yellow
    
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
        
        Write-Host "`n🎉 Ready to continue development!" -ForegroundColor Green
    } else {
        Write-Host "`n❌ BUILD FAILED!" -ForegroundColor Red
        Write-Host "Compilation errors found:" -ForegroundColor Red
        Write-Host $buildResult -ForegroundColor Red
        Write-Host "`n🔧 Please fix the errors before continuing." -ForegroundColor Red
    }
    
    Write-Host "`n=========================" -ForegroundColor Yellow
}

# Export functions for use in other scripts
Export-ModuleMember -Function Request-BuildCheck, Invoke-DotNetBuild

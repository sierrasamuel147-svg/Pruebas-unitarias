<#
.SYNOPSIS
    Runs the full test suite with Coverlet and builds an HTML coverage report.

.DESCRIPTION
    1. Deletes TestResults/ and CoverageReport/ so old runs are never mixed in.
    2. Restores local .NET tools (ReportGenerator 5.3.11 from .config/dotnet-tools.json).
    3. Runs dotnet test with the "XPlat Code Coverage" collector (coverlet.collector).
    4. Locates every coverage.cobertura.xml produced under TestResults/.
    5. Generates Html, HtmlSummary, Cobertura and TextSummary reports in CoverageReport/.

    Any failing step stops the script with a non-zero exit code.

.EXAMPLE
    .\scripts\coverage.ps1
#>

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$testResultsDir = Join-Path $repoRoot 'TestResults'
$coverageReportDir = Join-Path $repoRoot 'CoverageReport'

function Invoke-Step {
    param(
        [Parameter(Mandatory)] [string] $Description,
        [Parameter(Mandatory)] [scriptblock] $Command
    )

    Write-Host ""
    Write-Host "==> $Description" -ForegroundColor Cyan

    & $Command

    if ($LASTEXITCODE -ne 0) {
        throw "Step failed (exit code $LASTEXITCODE): $Description"
    }
}

function Remove-GeneratedDirectory {
    param([Parameter(Mandatory)] [string] $Path)

    if (Test-Path -LiteralPath $Path) {
        Write-Host "Removing $Path"
        Remove-Item -LiteralPath $Path -Recurse -Force
    }
}

Push-Location $repoRoot
try {
    Write-Host "==> Cleaning previous results" -ForegroundColor Cyan
    Remove-GeneratedDirectory $testResultsDir
    Remove-GeneratedDirectory $coverageReportDir

    Invoke-Step 'Restoring local .NET tools' {
        dotnet tool restore
    }

    Invoke-Step 'Running tests with Coverlet (XPlat Code Coverage)' {
        dotnet test TestingPlayground.sln `
            --collect:"XPlat Code Coverage" `
            --results-directory $testResultsDir
    }

    $coverageFiles = @(Get-ChildItem -Path $testResultsDir -Recurse -Filter 'coverage.cobertura.xml')
    if ($coverageFiles.Count -eq 0) {
        throw "No coverage.cobertura.xml was found under $testResultsDir."
    }

    Write-Host ""
    Write-Host "Coverage files found:"
    $coverageFiles | ForEach-Object { Write-Host "  $($_.FullName)" }

    $reports = ($coverageFiles | ForEach-Object { $_.FullName }) -join ';'

    Invoke-Step 'Generating report with ReportGenerator' {
        dotnet reportgenerator `
            "-reports:$reports" `
            "-targetdir:$coverageReportDir" `
            "-reporttypes:Html;HtmlSummary;Cobertura;TextSummary"
    }

    $indexFile = Join-Path $coverageReportDir 'index.html'
    if (-not (Test-Path -LiteralPath $indexFile)) {
        throw "ReportGenerator finished but $indexFile was not created."
    }

    $summaryFile = Join-Path $coverageReportDir 'Summary.txt'
    if (Test-Path -LiteralPath $summaryFile) {
        Write-Host ""
        Get-Content -LiteralPath $summaryFile -TotalCount 16 | ForEach-Object { Write-Host $_ }
    }

    Write-Host ""
    Write-Host "Tests: PASS" -ForegroundColor Green
    Write-Host "Coverage XML: generated ($($coverageFiles.Count) file(s))" -ForegroundColor Green
    Write-Host "HTML report: generated" -ForegroundColor Green
    Write-Host ""
    Write-Host "Open:"
    Write-Host "  CoverageReport/index.html"
}
finally {
    Pop-Location
}

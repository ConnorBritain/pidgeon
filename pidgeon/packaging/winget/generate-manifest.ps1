#!/usr/bin/env pwsh

param(
    [Parameter(Mandatory)]
    [string]$Version,

    [Parameter(Mandatory)]
    [string]$WinX64Sha256,

    [Parameter(Mandatory)]
    [string]$WinArm64Sha256,

    [string]$OutputDir = ".",

    [switch]$Submit
)

$ErrorActionPreference = "Stop"

# Load template
$templatePath = Join-Path $PSScriptRoot "manifest.yaml"
if (-not (Test-Path $templatePath)) {
    throw "Manifest template not found at: $templatePath"
}

$template = Get-Content $templatePath -Raw

# Replace placeholders
$manifest = $template `
    -replace '\$\{VERSION\}', $Version `
    -replace '\$\{WIN_X64_SHA256\}', $WinX64Sha256 `
    -replace '\$\{WIN_ARM64_SHA256\}', $WinArm64Sha256

# Ensure output directory exists
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

# Write manifest
$manifestPath = Join-Path $OutputDir "PidgeonHealth.CLI.yaml"
Set-Content -Path $manifestPath -Value $manifest -NoNewline

Write-Host "✅ Generated winget manifest: $manifestPath" -ForegroundColor Green

if ($Submit) {
    Write-Host "🚀 Submitting to winget-pkgs repository..." -ForegroundColor Cyan

    # Check if wingetcreate is installed
    if (-not (Get-Command "wingetcreate" -ErrorAction SilentlyContinue)) {
        Write-Host "Installing wingetcreate..." -ForegroundColor Yellow
        dotnet tool install --global wingetcreate
    }

    try {
        # Submit to winget-pkgs via PR
        wingetcreate update PidgeonHealth.CLI `
            --version $Version `
            --urls "https://github.com/PidgeonHealth/pidgeon/releases/download/v$Version/pidgeon-win-x64.zip" `
                   "https://github.com/PidgeonHealth/pidgeon/releases/download/v$Version/pidgeon-win-arm64.zip" `
            --submit

        Write-Host "✅ Winget manifest submitted successfully!" -ForegroundColor Green
    }
    catch {
        Write-Warning "Failed to submit to winget-pkgs: $($_.Exception.Message)"
        Write-Host "Manual submission may be required." -ForegroundColor Yellow
        Write-Host "Generated manifest available at: $manifestPath" -ForegroundColor Cyan
    }
}

return $manifestPath
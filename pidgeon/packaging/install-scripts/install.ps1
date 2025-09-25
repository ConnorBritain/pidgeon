# Pidgeon CLI Windows Installer Script
# https://pidgeon.health

param(
    [string]$Version = "latest",
    [string]$InstallDir = "$env:LOCALAPPDATA\Pidgeon",
    [switch]$AddToPath = $true
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# Configuration
$GitHubRepo = "PidgeonHealth/pidgeon"
$Architecture = if ([Environment]::Is64BitOperatingSystem) { "x64" } else { "x86" }
$Platform = "win-$Architecture"

# Colors for output (PowerShell 5.1+ with ANSI support)
function Write-InfoMessage($Message) {
    Write-Host "==> $Message" -ForegroundColor Blue
}

function Write-SuccessMessage($Message) {
    Write-Host "✓ $Message" -ForegroundColor Green
}

function Write-WarningMessage($Message) {
    Write-Host "⚠ $Message" -ForegroundColor Yellow
}

function Write-ErrorMessage($Message) {
    Write-Host "✗ $Message" -ForegroundColor Red
    exit 1
}

function Get-LatestVersion {
    if ($Version -eq "latest") {
        Write-InfoMessage "Fetching latest version..."
        try {
            $response = Invoke-RestMethod -Uri "https://api.github.com/repos/$GitHubRepo/releases/latest" -Headers @{"User-Agent" = "Pidgeon-Installer"}
            $Version = $response.tag_name -replace "^v", ""
        }
        catch {
            Write-ErrorMessage "Failed to fetch latest version: $($_.Exception.Message)"
        }
    }
    Write-InfoMessage "Installing version: $Version"
    return $Version
}

function Download-Pidgeon {
    $tempDir = New-TemporaryFile | ForEach-Object { Remove-Item $_; New-Item -ItemType Directory -Path $_ }
    $archiveName = "pidgeon-$Platform.zip"
    $url = "https://github.com/$GitHubRepo/releases/download/v$Version/$archiveName"
    $checksumUrl = "https://github.com/$GitHubRepo/releases/download/v$Version/$archiveName.sha256"

    $archivePath = Join-Path $tempDir $archiveName
    $checksumPath = Join-Path $tempDir "$archiveName.sha256"

    Write-InfoMessage "Downloading Pidgeon CLI..."
    try {
        Invoke-WebRequest -Uri $url -OutFile $archivePath -UseBasicParsing
    }
    catch {
        Write-ErrorMessage "Download failed: $($_.Exception.Message)"
    }

    Write-InfoMessage "Downloading checksum..."
    try {
        Invoke-WebRequest -Uri $checksumUrl -OutFile $checksumPath -UseBasicParsing
    }
    catch {
        Write-ErrorMessage "Checksum download failed: $($_.Exception.Message)"
    }

    Write-InfoMessage "Verifying integrity..."
    $expectedHash = (Get-Content $checksumPath | Select-String $archiveName) -replace "\s+$archiveName$", ""
    $actualHash = (Get-FileHash -Path $archivePath -Algorithm SHA256).Hash.ToLower()

    if ($expectedHash -ne $actualHash) {
        Write-ErrorMessage "Checksum verification failed. Expected: $expectedHash, Got: $actualHash"
    }
    Write-SuccessMessage "Integrity verified"

    Write-InfoMessage "Extracting archive..."
    try {
        Expand-Archive -Path $archivePath -DestinationPath $tempDir -Force
    }
    catch {
        Write-ErrorMessage "Extraction failed: $($_.Exception.Message)"
    }

    return $tempDir
}

function Install-Pidgeon {
    param($TempDir)

    Write-InfoMessage "Installing Pidgeon CLI..."

    # Create install directory
    if (-not (Test-Path $InstallDir)) {
        New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
    }

    # Install binary
    $binarySource = Join-Path $TempDir "pidgeon.exe"
    $binaryTarget = Join-Path $InstallDir "pidgeon.exe"

    if (-not (Test-Path $binarySource)) {
        Write-ErrorMessage "Binary not found at $binarySource"
    }

    Copy-Item $binarySource $binaryTarget -Force
    Write-SuccessMessage "Binary installed to $binaryTarget"

    # Install channel marker
    $configDir = Join-Path $InstallDir "config"
    if (-not (Test-Path $configDir)) {
        New-Item -ItemType Directory -Path $configDir -Force | Out-Null
    }

    '{"channel": "direct"}' | Out-File -FilePath (Join-Path $configDir "install.json") -Encoding utf8

    # Install completions if available
    $completionsSource = Join-Path $TempDir "completions"
    if (Test-Path $completionsSource) {
        Write-InfoMessage "Installing shell completions..."
        $completionsTarget = Join-Path $InstallDir "completions"

        if (-not (Test-Path $completionsTarget)) {
            New-Item -ItemType Directory -Path $completionsTarget -Force | Out-Null
        }

        Copy-Item "$completionsSource\*" $completionsTarget -Recurse -Force
        Write-SuccessMessage "Shell completions installed"
    }

    return $binaryTarget
}

function Add-ToPath {
    param($InstallDir)

    if (-not $AddToPath) {
        return
    }

    Write-InfoMessage "Adding to PATH..."

    # Get current user PATH
    $currentPath = [Environment]::GetEnvironmentVariable("Path", [EnvironmentVariableTarget]::User)

    if ($currentPath -notlike "*$InstallDir*") {
        $newPath = if ($currentPath) { "$currentPath;$InstallDir" } else { $InstallDir }
        [Environment]::SetEnvironmentVariable("Path", $newPath, [EnvironmentVariableTarget]::User)
        Write-SuccessMessage "Added $InstallDir to user PATH"
        Write-InfoMessage "Restart your terminal or run 'refreshenv' to use pidgeon command"
    } else {
        Write-SuccessMessage "Install directory already in PATH"
    }
}

function Show-CompletionHelp {
    Write-Host ""
    Write-Host "PowerShell Completion Setup:" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Add to your PowerShell profile (\$PROFILE):"
    Write-Host "  & '$InstallDir\pidgeon.exe' completions powershell | Out-String | Invoke-Expression"
    Write-Host ""
    Write-Host "Or create profile entry:"
    Write-Host "  New-Item -Force \$PROFILE"
    Write-Host "  Add-Content \$PROFILE '& `"$InstallDir\pidgeon.exe`" completions powershell | Out-String | Invoke-Expression'"
}

function Test-Installation {
    param($BinaryPath)

    Write-InfoMessage "Testing installation..."
    try {
        $version = & $BinaryPath --version 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-SuccessMessage "Installation verified - pidgeon command is working"
            Write-Host "Installed version: $version"
        } else {
            Write-WarningMessage "Installation completed but pidgeon command test failed"
        }
    }
    catch {
        Write-WarningMessage "Could not test installation: $($_.Exception.Message)"
    }
}

# Main installation flow
function Main {
    Write-Host ""
    Write-Host "Pidgeon CLI Windows Installer" -ForegroundColor Cyan
    Write-Host "============================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Platform: $Platform" -ForegroundColor Gray
    Write-Host "Install Directory: $InstallDir" -ForegroundColor Gray
    Write-Host ""

    # Check requirements
    if (-not (Get-Command "Expand-Archive" -ErrorAction SilentlyContinue)) {
        Write-ErrorMessage "PowerShell 5.0+ with Expand-Archive is required"
    }

    try {
        # Installation steps
        $Version = Get-LatestVersion
        $tempDir = Download-Pidgeon
        $binaryPath = Install-Pidgeon $tempDir
        Add-ToPath $InstallDir

        Write-Host ""
        Write-SuccessMessage "Pidgeon CLI v$Version installed successfully!"
        Write-Host ""
        Write-Host "Quick Start:" -ForegroundColor Cyan
        Write-Host "  pidgeon --version"
        Write-Host "  pidgeon --help"
        Write-Host "  pidgeon generate message --type ADT^A01 --count 5"
        Write-Host ""
        Write-Host "Documentation: https://docs.pidgeon.health" -ForegroundColor Cyan
        Write-Host "Issues: https://github.com/$GitHubRepo/issues" -ForegroundColor Cyan

        Show-CompletionHelp
        Test-Installation $binaryPath

    } catch {
        Write-ErrorMessage "Installation failed: $($_.Exception.Message)"
    } finally {
        # Cleanup
        if ($tempDir -and (Test-Path $tempDir)) {
            Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

# Run installer
Main
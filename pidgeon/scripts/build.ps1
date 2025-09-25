#!/usr/bin/env pwsh

param(
    [Parameter()]
    [string]$Configuration = "Release",

    [Parameter()]
    [string]$Version = "0.1.0",

    [Parameter()]
    [string[]]$Rids = @(
        "win-x64",
        "win-arm64",
        "linux-x64",
        "linux-arm64",
        "osx-x64",
        "osx-arm64"
    ),

    [Parameter()]
    [switch]$Clean,

    [Parameter()]
    [switch]$EnableAI
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# Colors for output
$Red = "`e[31m"
$Green = "`e[32m"
$Yellow = "`e[33m"
$Blue = "`e[34m"
$Magenta = "`e[35m"
$Cyan = "`e[36m"
$Reset = "`e[0m"

function Write-Step($Message) {
    Write-Host "${Blue}==>${Reset} ${Message}" -ForegroundColor Blue
}

function Write-Success($Message) {
    Write-Host "${Green}✓${Reset} ${Message}" -ForegroundColor Green
}

function Write-Warning($Message) {
    Write-Host "${Yellow}⚠${Reset} ${Message}" -ForegroundColor Yellow
}

function Write-Error($Message) {
    Write-Host "${Red}✗${Reset} ${Message}" -ForegroundColor Red
}

# Validate .NET SDK
Write-Step "Validating .NET SDK"
$dotnetVersion = dotnet --version 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Error ".NET SDK not found. Please install .NET 8.0 SDK."
    exit 1
}

if (-not $dotnetVersion.StartsWith("8.")) {
    Write-Warning "Expected .NET 8.x, found $dotnetVersion"
}
Write-Success ".NET SDK $dotnetVersion found"

# Set paths
$RootDir = $PSScriptRoot
$SrcDir = Join-Path $RootDir "src"
$CliProject = Join-Path $SrcDir "Pidgeon.CLI" "Pidgeon.CLI.csproj"
$DistDir = Join-Path $RootDir "dist"
$PackageDir = Join-Path $RootDir "packages"

# Clean previous builds
if ($Clean -or (Test-Path $DistDir)) {
    Write-Step "Cleaning previous builds"
    if (Test-Path $DistDir) { Remove-Item $DistDir -Recurse -Force }
    if (Test-Path $PackageDir) { Remove-Item $PackageDir -Recurse -Force }
    Write-Success "Cleaned build directories"
}

# Create output directories
New-Item -ItemType Directory -Path $DistDir -Force | Out-Null
New-Item -ItemType Directory -Path $PackageDir -Force | Out-Null

# Update version in Directory.Build.props
Write-Step "Setting version to $Version"
$buildPropsPath = Join-Path $RootDir "Directory.Build.props"
if (Test-Path $buildPropsPath) {
    $buildProps = Get-Content $buildPropsPath -Raw
    $buildProps = $buildProps -replace '<Version>.*</Version>', "<Version>$Version</Version>"
    $buildProps = $buildProps -replace '<AssemblyVersion>.*</AssemblyVersion>', "<AssemblyVersion>$Version.0</AssemblyVersion>"
    $buildProps = $buildProps -replace '<FileVersion>.*</FileVersion>', "<FileVersion>$Version.0</FileVersion>"
    $buildProps = $buildProps -replace '<InformationalVersion>.*</InformationalVersion>', "<InformationalVersion>$Version</InformationalVersion>"
    Set-Content $buildPropsPath $buildProps -NoNewline
}

# Build for each RID
Write-Step "Building self-contained binaries for $($Rids.Count) platforms"

$results = @()
foreach ($rid in $Rids) {
    Write-Host ""
    Write-Step "Building for $rid"

    $outputDir = Join-Path $DistDir "pidgeon-$rid"
    $archiveName = if ($rid.StartsWith("win")) { "pidgeon-$rid.zip" } else { "pidgeon-$rid.tar.gz" }
    $archivePath = Join-Path $PackageDir $archiveName

    try {
        # Build self-contained binary
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

        $aiParam = if ($EnableAI) { "/p:EnableAI=true" } else { "" }

        dotnet publish $CliProject `
            --configuration $Configuration `
            --runtime $rid `
            --output $outputDir `
            --self-contained true `
            --verbosity minimal `
            /p:Version=$Version `
            /p:PublishSingleFile=true `
            /p:EnableCompressionInSingleFile=true `
            /p:DebugType=embedded `
            /p:PublishTrimmed=false `
            /p:PublishReadyToRun=true `
            $aiParam

        if ($LASTEXITCODE -ne 0) {
            throw "dotnet publish failed with exit code $LASTEXITCODE"
        }

        $stopwatch.Stop()

        # Get binary info
        $binaryName = if ($rid.StartsWith("win")) { "pidgeon.exe" } else { "pidgeon" }
        $binaryPath = Join-Path $outputDir $binaryName

        if (-not (Test-Path $binaryPath)) {
            throw "Binary not found at $binaryPath"
        }

        $binarySize = [Math]::Round((Get-Item $binaryPath).Length / 1MB, 1)

        # Create archive
        if ($rid.StartsWith("win")) {
            # Windows: ZIP archive
            Compress-Archive -Path "$outputDir\*" -DestinationPath $archivePath -Force
        } else {
            # Unix: TAR.GZ archive
            Push-Location $outputDir
            try {
                tar -czf $archivePath *
            } finally {
                Pop-Location
            }
        }

        $archiveSize = [Math]::Round((Get-Item $archivePath).Length / 1MB, 1)

        $result = @{
            RID = $rid
            Success = $true
            BinarySize = $binarySize
            ArchiveSize = $archiveSize
            BuildTime = $stopwatch.Elapsed.TotalSeconds
            Archive = $archiveName
        }

        Write-Success "Built $rid ($($binarySize)MB binary, $($archiveSize)MB archive) in $([Math]::Round($stopwatch.Elapsed.TotalSeconds, 1))s"

    } catch {
        $result = @{
            RID = $rid
            Success = $false
            Error = $_.Exception.Message
        }
        Write-Error "Failed to build $rid`: $($_.Exception.Message)"
    }

    $results += $result
}

# Generate checksums
Write-Step "Generating checksums"
$checksumFile = Join-Path $PackageDir "checksums.txt"
$checksums = @()

Get-ChildItem $PackageDir -Filter "pidgeon-*" | ForEach-Object {
    $hash = (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLower()
    $checksums += "$hash  $($_.Name)"
}

$checksums | Set-Content $checksumFile
Write-Success "Generated checksums.txt with $($checksums.Count) entries"

# Build summary
Write-Host ""
Write-Step "Build Summary"
Write-Host ""

$successCount = ($results | Where-Object { $_.Success }).Count
$totalSize = ($results | Where-Object { $_.Success } | Measure-Object -Property ArchiveSize -Sum).Sum
$avgBuildTime = ($results | Where-Object { $_.Success } | Measure-Object -Property BuildTime -Average).Average

Write-Host "${Green}Successful builds:${Reset} $successCount/$($results.Count)"
Write-Host "${Blue}Total archive size:${Reset} $([Math]::Round($totalSize, 1))MB"
Write-Host "${Blue}Average build time:${Reset} $([Math]::Round($avgBuildTime, 1))s"

# Detailed results table
Write-Host ""
Write-Host "Platform Details:"
Write-Host "%-15s %-10s %-12s %-12s %-10s" -f "RID", "Status", "Binary(MB)", "Archive(MB)", "Time(s)"
Write-Host ("-" * 65)

foreach ($result in $results) {
    if ($result.Success) {
        $status = "${Green}✓${Reset}"
        Write-Host "%-15s %-10s %-12s %-12s %-10s" -f $result.RID, $status, $result.BinarySize, $result.ArchiveSize, [Math]::Round($result.BuildTime, 1)
    } else {
        $status = "${Red}✗${Reset}"
        Write-Host "%-15s %-10s %-12s" -f $result.RID, $status, $result.Error
    }
}

# Exit with error if any builds failed
$failedCount = ($results | Where-Object { -not $_.Success }).Count
if ($failedCount -gt 0) {
    Write-Host ""
    Write-Error "$failedCount build(s) failed"
    exit 1
} else {
    Write-Host ""
    Write-Success "All builds completed successfully!"
    Write-Host "${Cyan}Artifacts available in:${Reset} $PackageDir"
    exit 0
}
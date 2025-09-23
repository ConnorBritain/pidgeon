# HL7v23 File Name Normalization Script
# This script normalizes file naming conventions by removing v23_ prefixes

$basePath = "c:\Users\Connor.England.FUSIONMGT\OneDrive - Fusion\Documents\Code\CRE Code\hl7generator\pidgeon\data\standards\hl7v23"

Write-Host "Starting HL7v23 file name normalization..."

# Function to rename files with v23_ prefix
function Rename-V23Files {
    param(
        [string]$directory,
        [string]$directoryName
    )
    
    Write-Host "Processing $directoryName directory..."
    
    $files = Get-ChildItem -Path $directory -Filter "v23_*.json"
    $count = 0
    
    foreach ($file in $files) {
        $newName = $file.Name -replace "^v23_", ""
        
        # Special handling for tables directory
        if ($directoryName -eq "tables") {
            # Keep Pascal case for word-based table names and OSD1
            if ($newName -match "^[A-Za-z]") {
                # Keep as-is for word-based names like "City.json", "Country.json", etc.
                # and special cases like "OSD1.json"
            } else {
                # Convert numeric table names to lowercase
                $newName = $newName.ToLower()
            }
        } else {
            # Convert to lowercase for other directories
            $newName = $newName.ToLower()
        }
        
        $newPath = Join-Path $directory $newName
        
        # Check if target file already exists
        if (Test-Path $newPath) {
            Write-Warning "Target file already exists, skipping: $newName"
            continue
        }
        
        try {
            Rename-Item -Path $file.FullName -NewName $newName
            Write-Host "  Renamed: $($file.Name) -> $newName"
            $count++
        }
        catch {
            Write-Error "Failed to rename $($file.Name): $($_.Exception.Message)"
        }
    }
    
    Write-Host "  Processed $count files in $directoryName"
    return $count
}

# Process each directory
$totalRenamed = 0

# Data Types
$dataTypesPath = Join-Path $basePath "data_types"
$totalRenamed += Rename-V23Files -directory $dataTypesPath -directoryName "data_types"

# Segments  
$segmentsPath = Join-Path $basePath "segments"
$totalRenamed += Rename-V23Files -directory $segmentsPath -directoryName "segments"

# Tables (with special handling)
$tablesPath = Join-Path $basePath "tables"
$totalRenamed += Rename-V23Files -directory $tablesPath -directoryName "tables"

# Trigger Events
$triggerEventsPath = Join-Path $basePath "trigger_events"
$totalRenamed += Rename-V23Files -directory $triggerEventsPath -directoryName "trigger_events"

Write-Host ""
Write-Host "File normalization completed!"
Write-Host "Total files renamed: $totalRenamed"
Write-Host ""
Write-Host "Summary of changes:"
Write-Host "- Removed 'v23_' prefix from all applicable files"
Write-Host "- Converted most filenames to lowercase"
Write-Host "- Preserved Pascal case for word-based table names"
Write-Host "- Kept uppercase for special cases like OSD1.json"

#!/bin/bash

# Pidgeon Cross-Platform Build Script
# Builds self-contained executables for Windows, macOS, and Linux

set -e

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT_PATH="$PROJECT_ROOT/pidgeon/src/Pidgeon.CLI/Pidgeon.CLI.csproj"
DIST_DIR="$PROJECT_ROOT/pidgeon/dist"

echo "Building Pidgeon Healthcare Platform for all platforms..."
echo "Project: $PROJECT_PATH"
echo "Output: $DIST_DIR"
echo

# Clean previous builds
if [ -d "$DIST_DIR" ]; then
    echo "Cleaning previous builds..."
    rm -rf "$DIST_DIR"
fi

mkdir -p "$DIST_DIR"

# Build configuration
PLATFORMS=(
    "win-x64:Windows"
    "osx-x64:macOS Intel"
    "osx-arm64:macOS Apple Silicon"
    "linux-x64:Linux x64"
    "linux-arm64:Linux ARM64"
)

# Function to build for a platform
build_platform() {
    local runtime=$1
    local description=$2
    
    echo "Building for $description ($runtime)..."
    
    dotnet publish "$PROJECT_PATH" \
        --configuration Release \
        --runtime "$runtime" \
        --self-contained true \
        --output "$DIST_DIR/$runtime" \
        --verbosity quiet
    
    if [ $? -eq 0 ]; then
        # Get executable name
        if [[ "$runtime" == "win-"* ]]; then
            EXECUTABLE="Pidgeon.CLI.exe"
        else
            EXECUTABLE="Pidgeon.CLI"
        fi
        
        # Check file size
        SIZE=$(du -h "$DIST_DIR/$runtime/$EXECUTABLE" | cut -f1)
        echo "  Success: $EXECUTABLE ($SIZE)"
        
        # Create checksum
        cd "$DIST_DIR/$runtime"
        if [[ "$runtime" == "win-"* ]]; then
            # Windows - create SHA256 using PowerShell format
            echo "$(shasum -a 256 "$EXECUTABLE" | cut -d' ' -f1)  $EXECUTABLE" > "$EXECUTABLE.sha256"
        else
            shasum -a 256 "$EXECUTABLE" > "$EXECUTABLE.sha256"
        fi
        echo "  Checksum: $EXECUTABLE.sha256"
        cd - > /dev/null
    else
        echo "  Failed to build for $runtime"
        exit 1
    fi
    
    echo
}

# Build for all platforms
for platform_info in "${PLATFORMS[@]}"; do
    IFS=':' read -r runtime description <<< "$platform_info"
    build_platform "$runtime" "$description"
done

# Summary
echo "Build Summary:"
echo "=============="
find "$DIST_DIR" -name "Pidgeon.CLI*" -not -name "*.pdb" -not -name "*.sha256" | while read -r file; do
    SIZE=$(du -h "$file" | cut -f1)
    PLATFORM=$(echo "$file" | sed 's|.*/\([^/]*\)/[^/]*|\1|')
    FILENAME=$(basename "$file")
    echo "$PLATFORM: $FILENAME ($SIZE)"
done

echo
echo "Distribution files ready in: $DIST_DIR"
echo
echo "Test example:"
echo "  cd $DIST_DIR/osx-x64"
echo "  ./Pidgeon.CLI --version"
echo "  ./Pidgeon.CLI generate 'ADT^A01'"
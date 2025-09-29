#!/usr/bin/env bash

set -euo pipefail

# Default configuration
CONFIGURATION="${CONFIGURATION:-Release}"
VERSION="${VERSION:-0.1.0}"
RIDS=(
    "win-x64"
    "win-arm64"
    "linux-x64"
    "linux-arm64"
    "osx-x64"
    "osx-arm64"
)
CLEAN=false
ENABLE_AI=false

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -c|--configuration)
            CONFIGURATION="$2"
            shift 2
            ;;
        -v|--version)
            VERSION="$2"
            shift 2
            ;;
        --clean)
            CLEAN=true
            shift
            ;;
        --enable-ai)
            ENABLE_AI=true
            shift
            ;;
        -h|--help)
            echo "Usage: $0 [OPTIONS]"
            echo "Options:"
            echo "  -c, --configuration  Build configuration (default: Release)"
            echo "  -v, --version        Version number (default: 0.1.0)"
            echo "  --clean              Clean previous builds"
            echo "  --enable-ai          Enable AI features (includes LLamaSharp dependencies)"
            echo "  -h, --help           Show this help"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
BLUE='\033[0;34m'
MAGENTA='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

step() {
    echo -e "${BLUE}==>${NC} $1"
}

success() {
    echo -e "${GREEN}✓${NC} $1"
}

warning() {
    echo -e "${YELLOW}⚠${NC} $1"
}

error() {
    echo -e "${RED}✗${NC} $1"
}

# Validate .NET SDK
step "Validating .NET SDK"

# Check for WSL dotnet path first, then fall back to system dotnet
if [[ -f "/mnt/c/Program Files/dotnet/dotnet.exe" ]]; then
    DOTNET="/mnt/c/Program Files/dotnet/dotnet.exe"
elif command -v dotnet &> /dev/null; then
    DOTNET="dotnet"
else
    error ".NET SDK not found. Please install .NET 8.0 SDK."
    exit 1
fi

DOTNET_VERSION=$("$DOTNET" --version 2>/dev/null || echo "unknown")
if [[ ! $DOTNET_VERSION =~ ^8\. ]]; then
    warning "Expected .NET 8.x, found $DOTNET_VERSION"
fi
success ".NET SDK $DOTNET_VERSION found"

# Set paths
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SRC_DIR="$ROOT_DIR/src"
CLI_PROJECT="$SRC_DIR/Pidgeon.CLI/Pidgeon.CLI.csproj"
DIST_DIR="$ROOT_DIR/dist"
PACKAGE_DIR="$ROOT_DIR/packages"

# Clean previous builds
if [[ "$CLEAN" == true ]] || [[ -d "$DIST_DIR" ]]; then
    step "Cleaning previous builds"
    rm -rf "$DIST_DIR" "$PACKAGE_DIR"
    success "Cleaned build directories"
fi

# Create output directories
mkdir -p "$DIST_DIR" "$PACKAGE_DIR"

# Update version in Directory.Build.props
step "Setting version to $VERSION"
BUILD_PROPS="$ROOT_DIR/Directory.Build.props"
if [[ -f "$BUILD_PROPS" ]]; then
    # Use sed to update version fields
    sed -i.bak \
        -e "s|<Version>.*</Version>|<Version>$VERSION</Version>|g" \
        -e "s|<AssemblyVersion>.*</AssemblyVersion>|<AssemblyVersion>$VERSION.0</AssemblyVersion>|g" \
        -e "s|<FileVersion>.*</FileVersion>|<FileVersion>$VERSION.0</FileVersion>|g" \
        -e "s|<InformationalVersion>.*</InformationalVersion>|<InformationalVersion>$VERSION</InformationalVersion>|g" \
        "$BUILD_PROPS"
    rm -f "$BUILD_PROPS.bak"
fi

# Build for each RID
step "Building self-contained binaries for ${#RIDS[@]} platforms"

declare -a RESULTS=()
SUCCESS_COUNT=0
TOTAL_SIZE=0
TOTAL_TIME=0

for rid in "${RIDS[@]}"; do
    echo ""
    step "Building for $rid"

    OUTPUT_DIR="$DIST_DIR/pidgeon-$rid"
    if [[ $rid == win-* ]]; then
        ARCHIVE_NAME="pidgeon-$rid.zip"
    else
        ARCHIVE_NAME="pidgeon-$rid.tar.gz"
    fi
    ARCHIVE_PATH="$PACKAGE_DIR/$ARCHIVE_NAME"

    START_TIME=$(date +%s.%N)

    # Build dotnet publish command with conditional AI flag
    DOTNET_ARGS=(
        "$CLI_PROJECT"
        --configuration "$CONFIGURATION"
        --runtime "$rid"
        --output "$OUTPUT_DIR"
        --self-contained true
        --verbosity minimal
        "/p:Version=$VERSION"
        "/p:PublishSingleFile=true"
        "/p:EnableCompressionInSingleFile=true"
        "/p:DebugType=embedded"
        "/p:PublishTrimmed=false"
        "/p:PublishReadyToRun=true"
    )

    if [[ "$ENABLE_AI" == true ]]; then
        DOTNET_ARGS+=("/p:EnableAI=true")
    fi

    if "$DOTNET" publish "${DOTNET_ARGS[@]}"; then

        END_TIME=$(date +%s.%N)
        BUILD_TIME=$(echo "$END_TIME - $START_TIME" | bc -l)
        TOTAL_TIME=$(echo "$TOTAL_TIME + $BUILD_TIME" | bc -l)

        # Get binary info
        if [[ $rid == win-* ]]; then
            BINARY_NAME="Pidgeon.CLI.exe"
        else
            BINARY_NAME="Pidgeon.CLI"
        fi
        BINARY_PATH="$OUTPUT_DIR/$BINARY_NAME"

        if [[ -f "$BINARY_PATH" ]]; then
            BINARY_SIZE=$(du -m "$BINARY_PATH" | cut -f1)

            # Create archive
            pushd "$OUTPUT_DIR" >/dev/null
            if [[ $rid == win-* ]]; then
                # Windows: ZIP archive
                zip -r "$ARCHIVE_PATH" . >/dev/null 2>&1
            else
                # Unix: TAR.GZ archive
                tar -czf "$ARCHIVE_PATH" * >/dev/null 2>&1
            fi
            popd >/dev/null

            ARCHIVE_SIZE=$(du -m "$ARCHIVE_PATH" | cut -f1)
            TOTAL_SIZE=$((TOTAL_SIZE + ARCHIVE_SIZE))
            SUCCESS_COUNT=$((SUCCESS_COUNT + 1))

            success "Built $rid (${BINARY_SIZE}MB binary, ${ARCHIVE_SIZE}MB archive) in $(printf "%.1f" "$BUILD_TIME")s"

            RESULTS+=("$rid:SUCCESS:$BINARY_SIZE:$ARCHIVE_SIZE:$BUILD_TIME")
        else
            error "Binary not found at $BINARY_PATH"
            RESULTS+=("$rid:FAILED:Binary not found::$BUILD_TIME")
        fi
    else
        error "Failed to build $rid"
        RESULTS+=("$rid:FAILED:dotnet publish failed::")
    fi
done

# Generate checksums
step "Generating checksums"
CHECKSUM_FILE="$PACKAGE_DIR/checksums.txt"
> "$CHECKSUM_FILE"

find "$PACKAGE_DIR" -name "pidgeon-*" -type f | while read -r file; do
    if [[ "$file" != *"checksums.txt"* ]]; then
        HASH=$(shasum -a 256 "$file" | cut -d' ' -f1)
        BASENAME=$(basename "$file")
        echo "$HASH  $BASENAME" >> "$CHECKSUM_FILE"
    fi
done

CHECKSUM_COUNT=$(wc -l < "$CHECKSUM_FILE")
success "Generated checksums.txt with $CHECKSUM_COUNT entries"

# Build summary
echo ""
step "Build Summary"
echo ""

AVG_BUILD_TIME=0
if [[ $SUCCESS_COUNT -gt 0 ]]; then
    AVG_BUILD_TIME=$(echo "scale=1; $TOTAL_TIME / $SUCCESS_COUNT" | bc -l)
fi

echo -e "${GREEN}Successful builds:${NC} $SUCCESS_COUNT/${#RIDS[@]}"
echo -e "${BLUE}Total archive size:${NC} ${TOTAL_SIZE}MB"
echo -e "${BLUE}Average build time:${NC} $(printf "%.1f" "$AVG_BUILD_TIME")s"

# Detailed results table
echo ""
echo "Platform Details:"
printf "%-15s %-10s %-12s %-12s %-10s\n" "RID" "Status" "Binary(MB)" "Archive(MB)" "Time(s)"
echo "---------------------------------------------------------------"

for result in "${RESULTS[@]}"; do
    IFS=':' read -r rid status binary_size archive_size build_time <<< "$result"
    if [[ "$status" == "SUCCESS" ]]; then
        printf "%-15s ${GREEN}%-10s${NC} %-12s %-12s %-10s\n" "$rid" "✓" "$binary_size" "$archive_size" "$(printf "%.1f" "$build_time")"
    else
        printf "%-15s ${RED}%-10s${NC} %-12s\n" "$rid" "✗" "$binary_size"
    fi
done

# Exit with error if any builds failed
FAILED_COUNT=$((${#RIDS[@]} - SUCCESS_COUNT))
echo ""
if [[ $FAILED_COUNT -gt 0 ]]; then
    error "$FAILED_COUNT build(s) failed"
    exit 1
else
    success "All builds completed successfully!"
    echo -e "${CYAN}Artifacts available in:${NC} $PACKAGE_DIR"

    # Set up shell completions for the first available binary (typically linux-x64)
    echo ""
    info "Setting up shell completions..."
    FIRST_BINARY=""
    for rid in "${RIDS[@]}"; do
        BINARY_DIR="$PACKAGE_DIR/pidgeon-$rid"
        if [[ $rid == win-* ]]; then
            BINARY_NAME="pidgeon.exe"
        else
            BINARY_NAME="pidgeon"
        fi

        if [[ -f "$BINARY_DIR/$BINARY_NAME" ]]; then
            FIRST_BINARY="$BINARY_DIR/$BINARY_NAME"
            break
        fi
    done

    if [[ -n "$FIRST_BINARY" ]]; then
        SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
        if [[ -x "$SCRIPT_DIR/setup-completions.sh" ]]; then
            "$SCRIPT_DIR/setup-completions.sh" "$FIRST_BINARY" || warning "Shell completion setup failed"
        else
            warning "Shell completion setup script not found"
        fi
    else
        warning "No binary found for shell completion setup"
    fi

    exit 0
fi
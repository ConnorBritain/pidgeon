#!/bin/bash
set -euo pipefail

# Comprehensive build and test pipeline
# Usage: ./build-and-test.sh [OPTIONS]

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# Default configuration
CONFIGURATION="Release"
VERSION="0.1.0"
ENABLE_AI=false
SKIP_UNIT_TESTS=false
SKIP_BUILD_TESTS=false
TEST_PLATFORMS=("linux-x64")

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
        --enable-ai)
            ENABLE_AI=true
            shift
            ;;
        --all-platforms)
            TEST_PLATFORMS=("win-x64" "win-arm64" "linux-x64" "linux-arm64" "osx-x64" "osx-arm64")
            shift
            ;;
        --skip-unit-tests)
            SKIP_UNIT_TESTS=true
            shift
            ;;
        --skip-build-tests)
            SKIP_BUILD_TESTS=true
            shift
            ;;
        -h|--help)
            echo "Usage: $0 [OPTIONS]"
            echo "Options:"
            echo "  -c, --configuration  Build configuration (default: Release)"
            echo "  -v, --version        Version number (default: 0.1.0)"
            echo "  --enable-ai          Enable AI features"
            echo "  --all-platforms      Test all platforms (default: linux-x64 only)"
            echo "  --skip-unit-tests    Skip unit tests"
            echo "  --skip-build-tests   Skip build verification tests"
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

section() {
    echo ""
    echo -e "${MAGENTA}========================================${NC}"
    echo -e "${MAGENTA}$1${NC}"
    echo -e "${MAGENTA}========================================${NC}"
}

cd "$PROJECT_ROOT"

section "Phase 1: Unit Tests"
if [[ "$SKIP_UNIT_TESTS" == false ]]; then
    step "Running unit tests from pidgeon-tests"

    TESTS_DIR="../pidgeon-tests/unit/Pidgeon.Core.Tests"
    if [[ -d "$TESTS_DIR" ]]; then
        cd "$TESTS_DIR"

        if dotnet test --verbosity minimal --configuration "$CONFIGURATION"; then
            success "Unit tests passed"
        else
            error "Unit tests failed"
            exit 1
        fi

        cd "$PROJECT_ROOT"
    else
        warning "Unit tests directory not found at $TESTS_DIR"
    fi
else
    warning "Unit tests skipped"
fi

section "Phase 2: Build Binaries"
step "Building binaries with configuration: $CONFIGURATION"

BUILD_ARGS=("--configuration" "$CONFIGURATION" "--version" "$VERSION")
if [[ "$ENABLE_AI" == true ]]; then
    BUILD_ARGS+=("--enable-ai")
fi

if "./scripts/build.sh" "${BUILD_ARGS[@]}"; then
    success "Build completed successfully"
else
    error "Build failed"
    exit 1
fi

section "Phase 3: Build Verification Tests"
if [[ "$SKIP_BUILD_TESTS" == false ]]; then
    FAILED_PLATFORMS=()

    for platform in "${TEST_PLATFORMS[@]}"; do
        step "Testing platform: $platform"

        if [[ "$platform" == win-* ]]; then
            BINARY_PATH="./dist/pidgeon-$platform/pidgeon.exe"
        else
            BINARY_PATH="./dist/pidgeon-$platform/pidgeon"
        fi

        if [[ -f "$BINARY_PATH" ]]; then
            if "./scripts/test-build.sh" "$BINARY_PATH"; then
                success "Platform $platform: All tests passed"
            else
                error "Platform $platform: Some tests failed"
                FAILED_PLATFORMS+=("$platform")
            fi
        else
            error "Platform $platform: Binary not found at $BINARY_PATH"
            FAILED_PLATFORMS+=("$platform")
        fi
        echo ""
    done

    # Summary
    section "Build Verification Summary"

    SUCCESSFUL_COUNT=$((${#TEST_PLATFORMS[@]} - ${#FAILED_PLATFORMS[@]}))
    echo -e "${GREEN}Successful platforms:${NC} $SUCCESSFUL_COUNT/${#TEST_PLATFORMS[@]}"

    if [[ ${#FAILED_PLATFORMS[@]} -gt 0 ]]; then
        echo -e "${RED}Failed platforms:${NC} ${FAILED_PLATFORMS[*]}"
        exit 1
    else
        success "All build verification tests passed!"
    fi
else
    warning "Build verification tests skipped"
fi

section "Pipeline Complete"
success "All phases completed successfully!"
echo -e "${CYAN}Artifacts available in:${NC} ./dist"
echo -e "${CYAN}Packages available in:${NC} ./packages"
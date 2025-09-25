#!/usr/bin/env bash

set -euo pipefail

# Pidgeon CLI Installer Script
# https://pidgeon.health

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Configuration
GITHUB_REPO="PidgeonHealth/pidgeon"
INSTALL_DIR="${PIDGEON_INSTALL_DIR:-/usr/local/bin}"
CONFIG_DIR="${PIDGEON_CONFIG_DIR:-/usr/local/etc/pidgeon}"
COMPLETION_DIR_BASH="/etc/bash_completion.d"
COMPLETION_DIR_ZSH="/usr/local/share/zsh/site-functions"
COMPLETION_DIR_FISH="/usr/local/share/fish/vendor_completions.d"

# Override version if specified
VERSION="${PIDGEON_VERSION:-latest}"

# Functions
log_info() {
    echo -e "${BLUE}==>${NC} $1"
}

log_success() {
    echo -e "${GREEN}✓${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}⚠${NC} $1"
}

log_error() {
    echo -e "${RED}✗${NC} $1"
    exit 1
}

# Detect platform
detect_platform() {
    local os arch
    os=$(uname -s | tr '[:upper:]' '[:lower:]')
    arch=$(uname -m)

    case "$arch" in
        x86_64) arch="x64" ;;
        aarch64|arm64) arch="arm64" ;;
        *) log_error "Unsupported architecture: $arch" ;;
    esac

    case "$os" in
        linux) PLATFORM="linux-$arch" ;;
        darwin) PLATFORM="osx-$arch" ;;
        *) log_error "Unsupported operating system: $os" ;;
    esac

    log_info "Detected platform: $PLATFORM"
}

# Get latest version
get_latest_version() {
    if [[ "$VERSION" == "latest" ]]; then
        log_info "Fetching latest version..."
        VERSION=$(curl -fsSL "https://api.github.com/repos/$GITHUB_REPO/releases/latest" | grep '"tag_name":' | sed -E 's/.*"([^"]+)".*/\1/' | sed 's/^v//')
        if [[ -z "$VERSION" ]]; then
            log_error "Failed to fetch latest version"
        fi
    fi
    log_info "Installing version: $VERSION"
}

# Download and verify
download_pidgeon() {
    local url checksum_url temp_dir archive_name
    temp_dir=$(mktemp -d)
    archive_name="pidgeon-$PLATFORM.tar.gz"

    url="https://github.com/$GITHUB_REPO/releases/download/v$VERSION/$archive_name"
    checksum_url="https://github.com/$GITHUB_REPO/releases/download/v$VERSION/$archive_name.sha256"

    log_info "Downloading Pidgeon CLI..."
    curl -fsSL "$url" -o "$temp_dir/$archive_name" || log_error "Download failed"

    log_info "Downloading checksum..."
    curl -fsSL "$checksum_url" -o "$temp_dir/$archive_name.sha256" || log_error "Checksum download failed"

    log_info "Verifying integrity..."
    cd "$temp_dir"
    if ! shasum -a 256 -c "$archive_name.sha256" >/dev/null 2>&1; then
        log_error "Checksum verification failed"
    fi
    log_success "Integrity verified"

    log_info "Extracting archive..."
    tar -xzf "$archive_name" || log_error "Extraction failed"

    TEMP_DIR="$temp_dir"
}

# Install binary and completions
install_pidgeon() {
    log_info "Installing Pidgeon CLI..."

    # Create directories
    sudo mkdir -p "$INSTALL_DIR" "$CONFIG_DIR"

    # Install binary
    sudo cp "$TEMP_DIR/pidgeon" "$INSTALL_DIR/pidgeon"
    sudo chmod +x "$INSTALL_DIR/pidgeon"
    log_success "Binary installed to $INSTALL_DIR/pidgeon"

    # Install channel marker
    echo '{"channel": "direct"}' | sudo tee "$CONFIG_DIR/install.json" >/dev/null

    # Install completions if available
    if [[ -d "$TEMP_DIR/completions" ]]; then
        log_info "Installing shell completions..."

        # Bash completion
        if [[ -d "$COMPLETION_DIR_BASH" ]] && [[ -f "$TEMP_DIR/completions/pidgeon.bash" ]]; then
            sudo cp "$TEMP_DIR/completions/pidgeon.bash" "$COMPLETION_DIR_BASH/pidgeon"
            log_success "Bash completion installed"
        fi

        # Zsh completion
        if [[ -d "$COMPLETION_DIR_ZSH" ]] && [[ -f "$TEMP_DIR/completions/_pidgeon" ]]; then
            sudo cp "$TEMP_DIR/completions/_pidgeon" "$COMPLETION_DIR_ZSH/_pidgeon"
            log_success "Zsh completion installed"
        fi

        # Fish completion
        if [[ -d "$COMPLETION_DIR_FISH" ]] && [[ -f "$TEMP_DIR/completions/pidgeon.fish" ]]; then
            sudo cp "$TEMP_DIR/completions/pidgeon.fish" "$COMPLETION_DIR_FISH/pidgeon.fish"
            log_success "Fish completion installed"
        fi
    fi
}

# Cleanup
cleanup() {
    if [[ -n "${TEMP_DIR:-}" ]]; then
        rm -rf "$TEMP_DIR"
    fi
}

# Show completion instructions
show_completion_help() {
    echo ""
    echo -e "${CYAN}Shell Completion Setup:${NC}"
    echo ""
    echo "Bash (add to ~/.bashrc):"
    echo "  source /etc/bash_completion.d/pidgeon"
    echo ""
    echo "Zsh (completions should work automatically on next shell restart)"
    echo ""
    echo "Fish (completions should work automatically)"
    echo ""
    echo "Or generate completions manually:"
    echo "  pidgeon completions bash > ~/.pidgeon-completion.bash"
    echo "  pidgeon completions zsh > ~/.pidgeon-completion.zsh"
    echo "  pidgeon completions fish > ~/.config/fish/completions/pidgeon.fish"
}

# Main installation flow
main() {
    echo ""
    echo -e "${CYAN}Pidgeon CLI Installer${NC}"
    echo -e "${CYAN}=====================${NC}"
    echo ""

    # Check requirements
    if ! command -v curl >/dev/null; then
        log_error "curl is required but not installed"
    fi

    if ! command -v tar >/dev/null; then
        log_error "tar is required but not installed"
    fi

    if ! command -v shasum >/dev/null; then
        log_error "shasum is required but not installed"
    fi

    # Setup cleanup trap
    trap cleanup EXIT

    # Installation steps
    detect_platform
    get_latest_version
    download_pidgeon
    install_pidgeon

    echo ""
    log_success "Pidgeon CLI v$VERSION installed successfully!"
    echo ""
    echo -e "${CYAN}Quick Start:${NC}"
    echo "  pidgeon --version"
    echo "  pidgeon --help"
    echo "  pidgeon generate message --type ADT^A01 --count 5"
    echo ""
    echo -e "${CYAN}Documentation:${NC} https://docs.pidgeon.health"
    echo -e "${CYAN}Issues:${NC} https://github.com/$GITHUB_REPO/issues"

    show_completion_help

    # Test installation
    if pidgeon --version >/dev/null 2>&1; then
        echo ""
        log_success "Installation verified - pidgeon command is available"
    else
        echo ""
        log_warning "Installation completed but pidgeon command not found in PATH"
        log_info "You may need to add $INSTALL_DIR to your PATH or restart your shell"
    fi
}

main "$@"
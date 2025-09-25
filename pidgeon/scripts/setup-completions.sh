#!/usr/bin/env bash
set -euo pipefail 2>/dev/null || set -eu

# Pidgeon Shell Completion Setup Script
# Automatically sets up shell completions for supported shells
# Usage: ./setup-completions.sh [BINARY_PATH]

BINARY_PATH="${1:-./dist/pidgeon-linux-x64/Pidgeon.CLI}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

info() {
    echo -e "${BLUE}ℹ${NC} $1"
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

echo -e "${CYAN}🔧 PIDGEON SHELL COMPLETION SETUP${NC}"
echo "================================================"

# Check if binary exists
if [[ ! -f "$BINARY_PATH" ]]; then
    error "Binary not found at $BINARY_PATH"
    echo "Please build Pidgeon first or provide the correct binary path"
    exit 1
fi

if [[ ! -x "$BINARY_PATH" ]]; then
    error "Binary is not executable: $BINARY_PATH"
    exit 1
fi

info "Using binary: $(basename "$BINARY_PATH")"

# Create completions directory in dist
COMPLETIONS_DIR="$(dirname "$BINARY_PATH")/completions"
mkdir -p "$COMPLETIONS_DIR"

# Function to generate and save completion for a shell
setup_shell_completion() {
    local shell="$1"
    local output_file="$COMPLETIONS_DIR/pidgeon.$shell"

    info "Generating $shell completion..."

    if "$BINARY_PATH" completions "$shell" > "$output_file" 2>/dev/null; then
        success "Generated $shell completion: $output_file"
        return 0
    else
        error "Failed to generate $shell completion"
        return 1
    fi
}

# Generate completions for all supported shells
SHELLS=("bash" "zsh" "fish" "powershell")
GENERATED=0

for shell in "${SHELLS[@]}"; do
    if setup_shell_completion "$shell"; then
        ((GENERATED++))
    fi
done

echo ""
info "Generated $GENERATED shell completions in: $COMPLETIONS_DIR"

# Create installation instructions
INSTALL_SCRIPT="$COMPLETIONS_DIR/INSTALL.md"

cat > "$INSTALL_SCRIPT" << 'EOF'
# Pidgeon Shell Completion Installation

Shell completions have been generated for your convenience. Follow the instructions below for your shell:

## Bash

### System-wide installation (requires sudo):
```bash
sudo cp pidgeon.bash /etc/bash_completion.d/
```

### User installation:
```bash
mkdir -p ~/.local/share/bash-completion/completions
cp pidgeon.bash ~/.local/share/bash-completion/completions/
```

### Temporary (current session only):
```bash
source pidgeon.bash
```

## Zsh

### System-wide installation:
```bash
sudo cp pidgeon.zsh /usr/local/share/zsh/site-functions/_pidgeon
```

### User installation:
```bash
mkdir -p ~/.local/share/zsh/site-functions
cp pidgeon.zsh ~/.local/share/zsh/site-functions/_pidgeon
# Add to ~/.zshrc:
fpath=(~/.local/share/zsh/site-functions $fpath)
autoload -Uz compinit && compinit
```

### Oh My Zsh installation:
```bash
mkdir -p $ZSH_CUSTOM/plugins/pidgeon
cp pidgeon.zsh $ZSH_CUSTOM/plugins/pidgeon/_pidgeon
# Add 'pidgeon' to plugins array in ~/.zshrc
```

## Fish

### Installation:
```bash
mkdir -p ~/.config/fish/completions
cp pidgeon.fish ~/.config/fish/completions/
```

## PowerShell

### Installation:
```powershell
# Add to your PowerShell profile ($PROFILE):
. path/to/pidgeon.powershell
```

To find your profile path:
```powershell
echo $PROFILE
```

## Verification

After installation, restart your shell or source the appropriate configuration file.
Test completion by typing:

```bash
pidgeon <TAB><TAB>
```

You should see available commands and options.

## Auto-completion Features

Pidgeon completions provide:
- Command completion (generate, validate, find, etc.)
- Option completion (--help, --verbose, etc.)
- Smart argument completion for message types
- Field path completion for healthcare standards
- File path completion for input/output arguments

## Troubleshooting

If completions don't work:
1. Ensure the completion file is in the correct location
2. Restart your shell session
3. For bash: ensure bash-completion package is installed
4. For zsh: ensure compinit is run in your .zshrc
5. Check file permissions (should be readable)
EOF

success "Created installation guide: $INSTALL_SCRIPT"

# Check current shell and provide specific instructions
if [[ -n "${SHELL:-}" ]]; then
    CURRENT_SHELL="$(basename "$SHELL")"
    echo ""
    info "Detected shell: $CURRENT_SHELL"

    case "$CURRENT_SHELL" in
        bash)
            echo -e "${YELLOW}Quick install for bash:${NC}"
            echo "  source $COMPLETIONS_DIR/pidgeon.bash"
            ;;
        zsh)
            echo -e "${YELLOW}Quick install for zsh:${NC}"
            echo "  mkdir -p ~/.local/share/zsh/site-functions"
            echo "  cp $COMPLETIONS_DIR/pidgeon.zsh ~/.local/share/zsh/site-functions/_pidgeon"
            echo "  # Add to ~/.zshrc: fpath=(~/.local/share/zsh/site-functions \$fpath)"
            ;;
        fish)
            echo -e "${YELLOW}Quick install for fish:${NC}"
            echo "  mkdir -p ~/.config/fish/completions"
            echo "  cp $COMPLETIONS_DIR/pidgeon.fish ~/.config/fish/completions/"
            ;;
        *)
            echo -e "${YELLOW}See $INSTALL_SCRIPT for installation instructions${NC}"
            ;;
    esac
fi

echo ""
success "Shell completion setup completed!"
info "Generated files in: $COMPLETIONS_DIR"
warning "Remember to install completions to your shell's completion directory"
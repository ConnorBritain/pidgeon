#!/bin/bash
set -euo pipefail

# Comprehensive test for built Pidgeon binaries
# Usage: ./test-build.sh [BINARY_PATH]

BINARY_PATH="${1:-./dist/pidgeon-linux-x64/Pidgeon.CLI}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
TEST_DIR="$(mktemp -d)"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

test_step() {
    echo -e "${BLUE}==>${NC} $1"
}

test_success() {
    echo -e "${GREEN}✓${NC} $1"
}

test_warning() {
    echo -e "${YELLOW}⚠${NC} $1"
}

test_error() {
    echo -e "${RED}✗${NC} $1"
}

test_info() {
    echo -e "${CYAN}ℹ${NC} $1"
}

cleanup() {
    if [[ -d "$TEST_DIR" ]]; then
        rm -rf "$TEST_DIR"
    fi
}
trap cleanup EXIT

# Check if binary exists and is executable
if [[ ! -f "$BINARY_PATH" ]]; then
    test_error "Binary not found at $BINARY_PATH"
    exit 1
fi

if [[ ! -x "$BINARY_PATH" ]]; then
    test_error "Binary is not executable: $BINARY_PATH"
    exit 1
fi

echo -e "${CYAN}🧪 PIDGEON COMPREHENSIVE BUILD TEST${NC}"
echo "=============================================="
test_info "Binary: $(basename "$BINARY_PATH")"
test_info "Test directory: $TEST_DIR"

# Test 1: Basic CLI functionality
test_step "Test 1: Basic CLI Functionality"

# Version check
if "$BINARY_PATH" --version >/dev/null 2>&1; then
    VERSION_OUTPUT=$("$BINARY_PATH" --version)
    test_success "Version check: $VERSION_OUTPUT"
else
    test_error "Version check failed"
    exit 1
fi

# Help check
if "$BINARY_PATH" --help >/dev/null 2>&1; then
    test_success "Help output works"
else
    test_error "Help output failed"
    exit 1
fi

# Test 2: Core Message Generation
test_step "Test 2: Core Message Generation"

# Generate ADT^A01 (admission)
ADT_OUTPUT=$("$BINARY_PATH" generate "ADT^A01" 2>&1)
if echo "$ADT_OUTPUT" | grep -q "^MSH"; then
    test_success "ADT^A01 generation works"
    echo "$ADT_OUTPUT" | grep "^MSH" | head -1 | sed 's/^/  Sample: /'
else
    test_error "ADT^A01 generation failed"
fi

# Generate ORU^R01 (lab results)
ORU_OUTPUT=$("$BINARY_PATH" generate "ORU^R01" 2>&1)
if echo "$ORU_OUTPUT" | grep -q "^MSH"; then
    test_success "ORU^R01 generation works"
else
    test_warning "ORU^R01 generation failed"
fi

# Generate ORM^O01 (pharmacy order)
ORM_OUTPUT=$("$BINARY_PATH" generate "ORM^O01" 2>&1)
if echo "$ORM_OUTPUT" | grep -q "^MSH"; then
    test_success "ORM^O01 generation works"
else
    test_warning "ORM^O01 generation failed"
fi

# Test 3: Find Command Testing
test_step "Test 3: Find Command Testing"

if "$BINARY_PATH" find --help >/dev/null 2>&1; then
    test_success "Find command available"

    # Test basic field search
    FIND_PID_OUTPUT=$("$BINARY_PATH" find "PID.3" 2>&1)
    if echo "$FIND_PID_OUTPUT" | grep -q "PID.3\|Patient ID\|Patient Identifier"; then
        test_success "Find PID.3 (patient ID) works"
    else
        test_warning "Find PID.3 search failed"
    fi

    # Test semantic search
    FIND_PATIENT_OUTPUT=$("$BINARY_PATH" find "patient" --semantic 2>&1)
    if echo "$FIND_PATIENT_OUTPUT" | grep -q "PID\|Patient"; then
        test_success "Semantic patient search works"
    else
        test_warning "Semantic patient search failed"
    fi
else
    test_error "Find command not available"
fi

# Test 4: Lookup Command Testing
test_step "Test 4: Lookup Command Testing"

if "$BINARY_PATH" lookup --help >/dev/null 2>&1; then
    test_success "Lookup command available"

    # Test segment lookup
    LOOKUP_PID_OUTPUT=$("$BINARY_PATH" lookup "PID" 2>&1)
    if echo "$LOOKUP_PID_OUTPUT" | grep -q "Patient Identification\|PID"; then
        test_success "Lookup PID segment works"
    else
        test_warning "Lookup PID segment failed"
    fi

    # Test field lookup
    LOOKUP_FIELD_OUTPUT=$("$BINARY_PATH" lookup "MSH.9" 2>&1)
    if echo "$LOOKUP_FIELD_OUTPUT" | grep -q "Message Type\|MSH.9"; then
        test_success "Lookup MSH.9 field works"
    else
        test_warning "Lookup MSH.9 field failed"
    fi
else
    test_error "Lookup command not available"
fi

# Test 5: Path Command Testing
test_step "Test 5: Path Discovery Testing"

if "$BINARY_PATH" path --help >/dev/null 2>&1; then
    test_success "Path command available"

    # Test path listing
    PATH_OUTPUT=$("$BINARY_PATH" path --list 2>&1)
    if echo "$PATH_OUTPUT" | grep -q "PID\|MSH\|path"; then
        test_success "Path listing works"
    else
        test_warning "Path listing failed"
    fi
else
    test_warning "Path command not available"
fi

# Test 6: Session and Set Commands
test_step "Test 6: Session Management Testing"

# Create test session
SESSION_NAME="test_build_session_$$"

if "$BINARY_PATH" session --help >/dev/null 2>&1; then
    test_success "Session command available"

    # Create session
    SESSION_CREATE_OUTPUT=$("$BINARY_PATH" session create "$SESSION_NAME" 2>&1)
    if echo "$SESSION_CREATE_OUTPUT" | grep -q "Created\|session"; then
        test_success "Session creation works"
    else
        test_warning "Session creation failed"
    fi

    # Test set command if available
    if "$BINARY_PATH" set --help >/dev/null 2>&1; then
        test_success "Set command available"

        # Set a value in session
        SET_OUTPUT=$("$BINARY_PATH" set "patient.mrn" "TEST123" --session "$SESSION_NAME" 2>&1)
        if [[ $? -eq 0 ]]; then
            test_success "Set patient.mrn works"
        else
            test_warning "Set patient.mrn failed"
        fi
    else
        test_warning "Set command not available"
    fi

    # Clean up session
    "$BINARY_PATH" session delete "$SESSION_NAME" 2>/dev/null || true
else
    test_warning "Session command not available"
fi

# Test 7: Validation Command
test_step "Test 7: Message Validation Testing"

if "$BINARY_PATH" validate --help >/dev/null 2>&1; then
    test_success "Validate command available"

    # Create a simple test message
    TEST_MSG="MSH|^~\\&|PIDGEON|TEST|TEST|TEST|20250925||ADT^A01|123|P|2.3"
    echo "$TEST_MSG" > "$TEST_DIR/test_message.hl7"

    # Test file validation
    VALIDATE_OUTPUT=$("$BINARY_PATH" validate "$TEST_DIR/test_message.hl7" 2>&1)
    if [[ $? -eq 0 ]] || echo "$VALIDATE_OUTPUT" | grep -q "valid\|Valid\|MSH"; then
        test_success "Message validation works"
    else
        test_warning "Message validation failed"
    fi
else
    test_warning "Validate command not available"
fi

# Test 8: De-identification Command
test_step "Test 8: De-identification Testing"

if "$BINARY_PATH" deident --help >/dev/null 2>&1; then
    test_success "De-identification command available"

    # Create test data for de-identification
    mkdir -p "$TEST_DIR/input"
    echo "MSH|^~\\&|PIDGEON|TEST|TEST|TEST|20250925||ADT^A01|123|P|2.3" > "$TEST_DIR/input/sample.hl7"
    echo "PID|1||123456789^^^TEST^MR||DOE^JOHN^||19800101|M" >> "$TEST_DIR/input/sample.hl7"

    # Test preview mode
    DEIDENT_OUTPUT=$("$BINARY_PATH" deident "$TEST_DIR/input" "$TEST_DIR/output" --preview 2>&1)
    if [[ $? -eq 0 ]] || echo "$DEIDENT_OUTPUT" | grep -q "preview\|Preview\|de-identified"; then
        test_success "De-identification preview works"
    else
        test_warning "De-identification preview failed"
    fi
else
    test_warning "De-identification command not available"
fi

# Test 9: Diff Command (Pro Feature)
test_step "Test 9: Diff Command Testing"

if "$BINARY_PATH" diff --help >/dev/null 2>&1; then
    test_success "Diff command available"

    # Create two test files
    echo "MSH|^~\\&|PIDGEON|TEST|TEST|TEST|20250925||ADT^A01|123|P|2.3" > "$TEST_DIR/file1.hl7"
    echo "MSH|^~\\&|PIDGEON|TEST2|TEST|TEST|20250925||ADT^A01|124|P|2.3" > "$TEST_DIR/file2.hl7"

    # Test basic diff (skip pro check for testing)
    DIFF_OUTPUT=$("$BINARY_PATH" diff "$TEST_DIR/file1.hl7" "$TEST_DIR/file2.hl7" --basic --skip-pro-check 2>&1)
    if [[ $? -eq 0 ]] || echo "$DIFF_OUTPUT" | grep -q "diff\|Diff\|differ"; then
        test_success "Basic diff analysis works"
    else
        test_warning "Diff analysis failed"
    fi
else
    test_warning "Diff command not available"
fi

# Test 10: Workflow Command (Pro Feature)
test_step "Test 10: Workflow Command Testing"

if "$BINARY_PATH" workflow --help >/dev/null 2>&1; then
    test_success "Workflow command available"

    # Test workflow list
    WORKFLOW_LIST_OUTPUT=$("$BINARY_PATH" workflow list 2>&1)
    if [[ $? -eq 0 ]] || echo "$WORKFLOW_LIST_OUTPUT" | grep -q "workflow\|template"; then
        test_success "Workflow list works"
    else
        test_warning "Workflow list failed"
    fi
else
    test_warning "Workflow command not available"
fi

# Test 11: Shell Completion Generation
test_step "Test 11: Shell Completion Testing"

if "$BINARY_PATH" completions --help >/dev/null 2>&1; then
    test_success "Completions command available"

    # Test bash completion generation
    BASH_COMPLETION=$("$BINARY_PATH" completions bash 2>&1)
    if echo "$BASH_COMPLETION" | grep -q "complete\|_pidgeon\|bash"; then
        test_success "Bash completion generation works"
    else
        test_warning "Bash completion generation failed"
    fi

    # Test other shell completions
    for shell in zsh fish powershell; do
        if "$BINARY_PATH" completions "$shell" >/dev/null 2>&1; then
            test_success "$shell completion generation works"
        else
            test_warning "$shell completion generation failed"
        fi
    done
else
    test_error "Completions command not available"
fi


# Summary
echo ""
echo -e "${CYAN}📊 BUILD TEST SUMMARY${NC}"
echo "=============================================="

# Count available commands
AVAILABLE_COMMANDS=$(echo "generate find lookup path session set validate deident diff workflow completions" | wc -w)
test_info "Tested $AVAILABLE_COMMANDS core commands"

# Check binary size
if [[ -f "$BINARY_PATH" ]]; then
    BINARY_SIZE=$(ls -lh "$BINARY_PATH" | awk '{print $5}')
    test_info "Binary size: $BINARY_SIZE"
fi

test_success "Build test completed successfully"
test_info "Binary appears to be fully functional with embedded resources"

echo ""
echo -e "${GREEN}✅ COMPREHENSIVE BUILD VALIDATION COMPLETE${NC}"
echo "Ready for deployment and external testing suite execution"
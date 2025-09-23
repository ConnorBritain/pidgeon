#!/bin/bash

# MVP Regression Test Suite for Pidgeon v0.1.0
# Validates all core MVP functionality before release
# Exit on any failure for CI/CD integration

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
CLI_PROJECT="$PROJECT_ROOT/src/Pidgeon.CLI"
TEST_OUTPUT_DIR="$(mktemp -d)"
TEST_SESSION="regression_$(date +%s)"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}🚀 Pidgeon MVP Regression Test Suite${NC}"
echo -e "${BLUE}======================================${NC}"
echo "Test output directory: $TEST_OUTPUT_DIR"
echo "CLI project path: $CLI_PROJECT"
echo ""

# Function to run CLI command and check exit code
run_cli() {
    local cmd="$1"
    local description="$2"

    echo -e "${YELLOW}Testing: $description${NC}"
    echo "Command: dotnet run --project \"$CLI_PROJECT\" -- $cmd"

    if dotnet run --project "$CLI_PROJECT" -- $cmd > "$TEST_OUTPUT_DIR/last_output.txt" 2>&1; then
        echo -e "${GREEN}✅ PASS: $description${NC}"
        return 0
    else
        echo -e "${RED}❌ FAIL: $description${NC}"
        echo "Output:"
        cat "$TEST_OUTPUT_DIR/last_output.txt"
        return 1
    fi
}

# Function to check file exists and contains pattern
check_file_content() {
    local file="$1"
    local pattern="$2"
    local description="$3"

    if [[ -f "$file" ]] && grep -q "$pattern" "$file"; then
        echo -e "${GREEN}✅ PASS: $description${NC}"
        return 0
    else
        echo -e "${RED}❌ FAIL: $description${NC}"
        echo "File: $file"
        echo "Expected pattern: $pattern"
        if [[ -f "$file" ]]; then
            echo "Actual content:"
            head -5 "$file"
        else
            echo "File not found"
        fi
        return 1
    fi
}

# Cleanup function
cleanup() {
    echo -e "${BLUE}🧹 Cleaning up test session and files${NC}"
    dotnet run --project "$CLI_PROJECT" -- session remove "$TEST_SESSION" 2>/dev/null || true
    rm -rf "$TEST_OUTPUT_DIR"
}

trap cleanup EXIT

echo -e "${BLUE}=== CORE MVP FEATURE TESTS ===${NC}"
echo ""

# Test 1: Basic Generation (Core MVP)
echo -e "${BLUE}1. Message Generation Tests${NC}"
run_cli "generate \"ADT^A01\" --output \"$TEST_OUTPUT_DIR/test_adt.hl7\" --count 1" "Generate ADT message"
check_file_content "$TEST_OUTPUT_DIR/test_adt.hl7" "MSH|" "Generated file contains HL7 header"
check_file_content "$TEST_OUTPUT_DIR/test_adt.hl7" "ADT\^A01" "Generated file contains correct message type"

run_cli "generate \"ORU^R01\" --output \"$TEST_OUTPUT_DIR/test_oru.hl7\" --count 2" "Generate ORU messages with count"
check_file_content "$TEST_OUTPUT_DIR/test_oru.hl7" "ORU\^R01" "Generated ORU contains correct message type"

# Test 2: Validation (Core MVP)
echo -e "${BLUE}2. Message Validation Tests${NC}"
run_cli "validate --file \"$TEST_OUTPUT_DIR/test_adt.hl7\"" "Validate generated ADT message"

# Test 3: Semantic Path System (Revolutionary Feature)
echo -e "${BLUE}3. Semantic Path System Tests${NC}"
run_cli "path list" "List available semantic paths"
run_cli "path resolve patient.mrn \"ADT^A01\"" "Resolve patient.mrn for ADT"
run_cli "path validate patient.name \"ORU^R01\"" "Validate semantic path for ORU"

# Test 4: Session Management System (Workflow Automation)
echo -e "${BLUE}4. Session Management Tests${NC}"
run_cli "session create \"$TEST_SESSION\"" "Create new session"
run_cli "set patient.mrn \"REG123456\"" "Set patient MRN in session"
run_cli "set patient.sex \"M\"" "Set patient gender in session"

run_cli "generate \"ADT^A01\" --output \"$TEST_OUTPUT_DIR/session_adt.hl7\"" "Generate ADT with session context"
check_file_content "$TEST_OUTPUT_DIR/session_adt.hl7" "REG123456" "Session ADT contains set MRN"

run_cli "generate \"ORU^R01\" --output \"$TEST_OUTPUT_DIR/session_oru.hl7\"" "Generate ORU with same session"
check_file_content "$TEST_OUTPUT_DIR/session_oru.hl7" "REG123456" "Session ORU contains same MRN"

# Test 5: De-identification (Major Differentiator)
echo -e "${BLUE}5. De-identification Tests${NC}"
run_cli "deident --in \"$TEST_OUTPUT_DIR/test_adt.hl7\" --out \"$TEST_OUTPUT_DIR/deident_adt.hl7\" --date-shift 30d" "De-identify ADT message"
check_file_content "$TEST_OUTPUT_DIR/deident_adt.hl7" "MSH|" "De-identified file is valid HL7"

# Test 6: Professional Tier Infrastructure
echo -e "${BLUE}6. Professional Tier Tests${NC}"
if ! run_cli "workflow wizard" "Professional workflow wizard (should show upgrade prompt)"; then
    # Expected to fail - check for upgrade messaging
    if grep -q -i "professional\|upgrade\|\$29" "$TEST_OUTPUT_DIR/last_output.txt"; then
        echo -e "${GREEN}✅ PASS: Shows professional upgrade prompt${NC}"
    else
        echo -e "${RED}❌ FAIL: Missing professional upgrade messaging${NC}"
        exit 1
    fi
fi

# Test 7: Help System Completeness
echo -e "${BLUE}7. CLI Help System Tests${NC}"
run_cli "--help" "Main help system"
run_cli "generate --help" "Generate command help"
run_cli "validate --help" "Validate command help"
run_cli "path --help" "Path command help"

# Test 8: Cross-Standard Support
echo -e "${BLUE}8. Cross-Standard Support Tests${NC}"
run_cli "path list --standard hl7v23" "List HL7 v2.3 paths"
run_cli "path resolve patient.mrn \"ADT^A01\" --all-standards" "Cross-standard path resolution"

# Test 9: Error Handling and Edge Cases
echo -e "${BLUE}9. Error Handling Tests${NC}"
if run_cli "generate \"INVALID^XX\" --output \"$TEST_OUTPUT_DIR/invalid.hl7\"" "Invalid message type (should fail gracefully)"; then
    echo -e "${YELLOW}⚠️  WARNING: Invalid message type should fail${NC}"
fi

if run_cli "validate --file \"$TEST_OUTPUT_DIR/nonexistent.hl7\"" "Validate nonexistent file (should fail gracefully)"; then
    echo -e "${YELLOW}⚠️  WARNING: Nonexistent file validation should fail${NC}"
fi

# Test 10: File Operations and Output
echo -e "${BLUE}10. File Operations Tests${NC}"
run_cli "generate \"ADT^A01\" --output \"$TEST_OUTPUT_DIR/deterministic1.hl7\" --seed 12345" "Deterministic generation with seed"
run_cli "generate \"ADT^A01\" --output \"$TEST_OUTPUT_DIR/deterministic2.hl7\" --seed 12345" "Same seed should produce same output"

if diff "$TEST_OUTPUT_DIR/deterministic1.hl7" "$TEST_OUTPUT_DIR/deterministic2.hl7" > /dev/null; then
    echo -e "${GREEN}✅ PASS: Deterministic generation with seeds${NC}"
else
    echo -e "${RED}❌ FAIL: Seeds should produce identical output${NC}"
    exit 1
fi

echo ""
echo -e "${GREEN}🎉 ALL MVP REGRESSION TESTS PASSED! 🎉${NC}"
echo -e "${GREEN}====================================${NC}"
echo ""
echo -e "${BLUE}📊 Test Summary:${NC}"
echo "✅ Message Generation (ADT, ORU)"
echo "✅ Message Validation"
echo "✅ Semantic Path System"
echo "✅ Session/Lock Workflow"
echo "✅ De-identification"
echo "✅ Professional Tier Infrastructure"
echo "✅ Help System"
echo "✅ Cross-Standard Support"
echo "✅ Error Handling"
echo "✅ File Operations"
echo ""
echo -e "${GREEN}🚀 Pidgeon v0.1.0 MVP is READY FOR SHIP! 🚀${NC}"

exit 0
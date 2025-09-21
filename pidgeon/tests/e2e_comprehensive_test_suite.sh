#!/bin/bash

# Pidgeon E2E Comprehensive Test Suite
# This script validates all Sprint 1 achievements and core CLI functionality
# Run with: bash tests/e2e_comprehensive_test_suite.sh

set -e

echo "🚀 PIDGEON E2E COMPREHENSIVE TEST SUITE"
echo "======================================="
echo ""

# Configuration
CLI_CMD="/mnt/c/Program\ Files/dotnet/dotnet.exe run --project src/Pidgeon.CLI --"
TEST_SESSION="e2e_test_$(date +%s)"
TEMP_FILES=()

# Cleanup function
cleanup() {
    echo ""
    echo "🧹 Cleaning up test session and files..."
    eval "$CLI_CMD lock remove $TEST_SESSION" 2>/dev/null || true
    for file in "${TEMP_FILES[@]}"; do
        rm -f "$file" 2>/dev/null || true
    done
    echo "✅ Cleanup complete"
}

# Trap cleanup on exit
trap cleanup EXIT

# Helper functions
run_test() {
    local test_name="$1"
    local command="$2"
    local expected_pattern="$3"

    echo "🧪 TEST: $test_name"
    echo "   Command: $command"

    if output=$(eval "$command" 2>&1); then
        if [[ -z "$expected_pattern" ]] || echo "$output" | grep -q "$expected_pattern"; then
            echo "   ✅ PASS"
            return 0
        else
            echo "   ❌ FAIL - Expected pattern not found: $expected_pattern"
            echo "   Output: $output"
            return 1
        fi
    else
        echo "   ❌ FAIL - Command failed"
        echo "   Error: $output"
        return 1
    fi
    echo ""
}

fail_test() {
    echo "💥 TEST SUITE FAILED"
    exit 1
}

echo "=== BASIC CLI FUNCTIONALITY ==="

# Test 1: Version Command
run_test "Version Command" "$CLI_CMD --version" "1.0.0" || fail_test

# Test 2: Help Command
run_test "Help Command" "$CLI_CMD --help" "Usage:" || fail_test

# Test 3: Generate Basic Message
run_test "Generate ADT^A01" "$CLI_CMD generate 'ADT^A01'" "MSH|" || fail_test

# Test 4: Generate with Count
temp_file="test_count_$(date +%s).hl7"
TEMP_FILES+=("$temp_file")
run_test "Generate Multiple Messages" "$CLI_CMD generate 'ADT^A01' --count 3 --output '$temp_file'" "Generated 3" || fail_test

# Test 5: Validation
run_test "Validate Generated Message" "$CLI_CMD validate --file '$temp_file'" "Validation passed" || fail_test

# Test 6: De-identification Preview
run_test "De-identification Preview" "$CLI_CMD deident --in '$temp_file' --preview" "PHI detection" || fail_test

echo "=== LOOKUP SYSTEM ==="

# Test 7: Segment Lookup
run_test "Lookup PID Segment" "$CLI_CMD lookup PID" "Patient Identification" || fail_test

# Test 8: Field Lookup
run_test "Lookup PID.3 Field" "$CLI_CMD lookup PID.3" "Patient Identifier List" || fail_test

# Test 9: Table Lookup
run_test "Lookup Table 0001" "$CLI_CMD lookup 0001" "Administrative Sex" || fail_test

echo "=== SPRINT 1 ADVANCED FEATURES ==="

# Test 10: Lock Session Creation
run_test "Create Lock Session" "$CLI_CMD lock create '$TEST_SESSION'" "Created lock session" || fail_test

# Test 11: Lock Session List
run_test "List Lock Sessions" "$CLI_CMD lock list" "$TEST_SESSION" || fail_test

# Test 12: Semantic Path Setting
run_test "Set Semantic Path patient.mrn" "$CLI_CMD set '$TEST_SESSION' patient.mrn 'MR123456'" "Set value in session" || fail_test

# Test 13: Additional Semantic Path
run_test "Set Semantic Path patient.sex" "$CLI_CMD set '$TEST_SESSION' patient.sex 'M'" "Set value in session" || fail_test

# Test 14: Lock Session Values List
run_test "List Session Values" "$CLI_CMD set '$TEST_SESSION' --list" "patient.mrn" || fail_test

# Test 15: Lock-Aware Generation (ADT)
temp_adt="test_lock_adt_$(date +%s).hl7"
TEMP_FILES+=("$temp_adt")
run_test "Lock-Aware ADT Generation" "$CLI_CMD generate 'ADT^A01' --use-lock '$TEST_SESSION' --output '$temp_adt'" "Using lock session" || fail_test

# Test 16: Verify Locked Values in Generated Message
run_test "Verify MRN in Generated ADT" "grep 'MR123456' '$temp_adt'" "MR123456" || fail_test
run_test "Verify Gender in Generated ADT" "grep '|M' '$temp_adt'" "|M" || fail_test

# Test 17: Patient Journey Continuity (Different Message Type)
temp_oru="test_lock_oru_$(date +%s).hl7"
TEMP_FILES+=("$temp_oru")
run_test "Lock-Aware ORU Generation" "$CLI_CMD generate 'ORU^R01' --use-lock '$TEST_SESSION' --output '$temp_oru'" "Using lock session" || fail_test

# Test 18: Verify Continuity Across Message Types
run_test "Verify MRN Continuity in ORU" "grep 'MR123456' '$temp_oru'" "MR123456" || fail_test
run_test "Verify Gender Continuity in ORU" "grep '|M' '$temp_oru'" "|M" || fail_test

echo "=== SEMANTIC PATH SYSTEM ==="

# Test 19: Path Resolution
run_test "Path Resolution patient.mrn" "$CLI_CMD path resolve patient.mrn 'ADT^A01'" "PID.3" || fail_test

# Test 20: Path Validation
run_test "Path Validation" "$CLI_CMD path validate patient.mrn 'ADT^A01'" "Path is valid" || fail_test

echo "=== PROFESSIONAL TIER FEATURES ==="

# Test 21: Diff Command (with Pro bypass)
if [ -f "$temp_adt" ] && [ -f "$temp_oru" ]; then
    run_test "Diff Analysis with AI" "$CLI_CMD diff '$temp_adt' '$temp_oru' --skip-pro-check" "differences" || fail_test
fi

# Test 22: Workflow Wizard (Pro Gating)
run_test "Workflow Wizard Pro Gating" "$CLI_CMD workflow wizard" "Professional" || fail_test

echo "=== DATA FOUNDATION VALIDATION ==="

# Test 23: Standards Data Available
run_test "Demographics Data Available" "$CLI_CMD lookup FirstName" "FirstName" || fail_test

# Test 24: Multiple Standards Support
run_test "HL7 v2.3 Support" "$CLI_CMD lookup --segments --standard hl7v23" "segments" || fail_test

echo ""
echo "🎉 ALL TESTS PASSED!"
echo "======================================="
echo ""
echo "📊 VALIDATION SUMMARY:"
echo "✅ Basic CLI: Generate, Validate, De-identify working"
echo "✅ Lookup System: Segments, fields, tables accessible"
echo "✅ Lock/Set System: Session management working"
echo "✅ Semantic Paths: Cross-standard patient.mrn → PID.3 working"
echo "✅ Workflow Continuity: Same patient across message types"
echo "✅ Path Discovery: Resolution and validation working"
echo "✅ Professional Features: Diff analysis, Pro gating working"
echo "✅ Data Foundation: Demographics and standards data accessible"
echo ""
echo "🚀 SPRINT 1 ACHIEVEMENTS VALIDATED: Platform ready for immediate ship!"
echo ""
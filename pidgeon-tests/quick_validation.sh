#!/bin/bash

# Quick Pidgeon Validation Script
# Tests core MVP functionality in under 30 seconds
# Updated for proper session management and current CLI structure

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CLI_PROJECT="$SCRIPT_DIR/../src/Pidgeon.CLI"
SESSION="quicktest_$(date +%s)"
TEMP_DIR="$(mktemp -d)"

echo "🚀 PIDGEON QUICK VALIDATION"
echo "============================"

echo "1/5 Testing core generation..."
dotnet run --project "$CLI_PROJECT" -- generate "ADT^A01" --output "$TEMP_DIR/test.hl7" --count 1 > /dev/null
if grep -q "MSH|" "$TEMP_DIR/test.hl7"; then
    echo "✅ Generation: PASS"
else
    echo "❌ Generation: FAIL"
    exit 1
fi

echo "2/5 Testing validation..."
if dotnet run --project "$CLI_PROJECT" -- validate --file "$TEMP_DIR/test.hl7" > /dev/null 2>&1; then
    echo "✅ Validation: PASS"
else
    echo "❌ Validation: FAIL"
    exit 1
fi

echo "3/5 Testing semantic paths..."
if dotnet run --project "$CLI_PROJECT" -- path list > /dev/null 2>&1; then
    echo "✅ Semantic Paths: PASS"
else
    echo "❌ Semantic Paths: FAIL"
    exit 1
fi

echo "4/5 Testing session workflow..."
dotnet run --project "$CLI_PROJECT" -- session create "$SESSION" > /dev/null 2>&1
dotnet run --project "$CLI_PROJECT" -- set patient.mrn "QUICK123" > /dev/null 2>&1
dotnet run --project "$CLI_PROJECT" -- generate "ADT^A01" --output "$TEMP_DIR/session_test.hl7" > /dev/null 2>&1
if grep -q "QUICK123" "$TEMP_DIR/session_test.hl7"; then
    echo "✅ Session Workflow: PASS"
    dotnet run --project "$CLI_PROJECT" -- session remove "$SESSION" > /dev/null 2>&1 || true
else
    echo "❌ Session Workflow: FAIL"
    exit 1
fi

echo "5/5 Testing help system..."
if dotnet run --project "$CLI_PROJECT" -- --help > /dev/null 2>&1; then
    echo "✅ Help System: PASS"
else
    echo "❌ Help System: FAIL"
    exit 1
fi

# Cleanup
rm -rf "$TEMP_DIR"

echo ""
echo "🎉 ALL QUICK TESTS PASSED!"
echo "💡 Run tests/regression/mvp_regression_suite.sh for full validation"
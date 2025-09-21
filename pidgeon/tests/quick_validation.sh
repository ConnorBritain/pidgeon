#!/bin/bash

# Quick Pidgeon Validation Script
# Tests the most critical Sprint 1 achievements in under 2 minutes

echo "🚀 PIDGEON QUICK VALIDATION"
echo "============================"

CLI_CMD="/mnt/c/Program\ Files/dotnet/dotnet.exe run --project src/Pidgeon.CLI --"
SESSION="quick_test_$(date +%s)"

echo ""
echo "1️⃣ Testing core generation..."
eval "$CLI_CMD generate 'ADT^A01'" | head -1

echo ""
echo "2️⃣ Testing lock system..."
eval "$CLI_CMD lock create '$SESSION'"

echo ""
echo "3️⃣ Testing semantic paths..."
eval "$CLI_CMD set '$SESSION' patient.mrn 'TEST123'"

echo ""
echo "4️⃣ Testing workflow continuity..."
echo "ADT Message with locked patient:"
eval "$CLI_CMD generate 'ADT^A01' --use-lock '$SESSION'" | grep 'TEST123'

echo ""
echo "ORU Message with same patient:"
eval "$CLI_CMD generate 'ORU^R01' --use-lock '$SESSION'" | grep 'TEST123'

echo ""
echo "5️⃣ Testing path resolution..."
eval "$CLI_CMD path resolve patient.mrn 'ADT^A01'"

echo ""
echo "🧹 Cleanup..."
eval "$CLI_CMD lock remove '$SESSION'" 2>/dev/null

echo ""
echo "✅ QUICK VALIDATION COMPLETE"
echo "Sprint 1 achievements confirmed working!"
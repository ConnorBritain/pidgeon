#!/bin/bash
set -euo pipefail

# Update Homebrew formula with new version and checksums
# Usage: ./update-formula.sh VERSION CHECKSUM_FILE

VERSION="${1:-}"
CHECKSUM_FILE="${2:-}"

if [[ -z "$VERSION" ]] || [[ -z "$CHECKSUM_FILE" ]]; then
    echo "Usage: $0 VERSION CHECKSUM_FILE"
    echo "Example: $0 0.1.0 ../../packages/checksums.txt"
    exit 1
fi

FORMULA_FILE="pidgeon.rb"
TEMP_FORMULA=$(mktemp)

# Read checksums from file
declare -A CHECKSUMS
while IFS= read -r line; do
    if [[ $line =~ ^([a-f0-9]+)[[:space:]]+pidgeon-([^.]+)\.tar\.gz$ ]]; then
        CHECKSUMS["${BASH_REMATCH[2]}"]="${BASH_REMATCH[1]}"
    fi
done < "$CHECKSUM_FILE"

# Update formula
sed "s/version \".*\"/version \"$VERSION\"/" "$FORMULA_FILE" > "$TEMP_FORMULA"

# Update checksums for each platform
for platform in "osx-arm64" "osx-x64" "linux-arm64" "linux-x64"; do
    if [[ -n "${CHECKSUMS[$platform]:-}" ]]; then
        sed -i "s/placeholder-sha256-for-${platform/osx-/macos-}/${CHECKSUMS[$platform]}/" "$TEMP_FORMULA"
    fi
done

# Replace original formula
mv "$TEMP_FORMULA" "$FORMULA_FILE"

echo "✓ Updated $FORMULA_FILE with version $VERSION and checksums"
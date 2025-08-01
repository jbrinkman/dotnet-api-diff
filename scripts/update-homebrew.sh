#!/bin/bash

set -euo pipefail

# Script to update Homebrew formula with new version and checksums

VERSION="${1:-}"
SHA256_OSX_ARM64="${2:-}"
SHA256_OSX_X64="${3:-}"
SHA256_LINUX_ARM64="${4:-}"
SHA256_LINUX_X64="${5:-}"

if [[ -z "$VERSION" || -z "$SHA256_OSX_ARM64" || -z "$SHA256_OSX_X64" || -z "$SHA256_LINUX_ARM64" || -z "$SHA256_LINUX_X64" ]]; then
    echo "Usage: $0 <version> <sha256_osx_arm64> <sha256_osx_x64> <sha256_linux_arm64> <sha256_linux_x64>"
    echo "Example: $0 1.0.0 abc123... def456... ghi789... jkl012..."
    exit 1
fi

echo "Updating Homebrew formula for version $VERSION"

# Paths
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
PACKAGING_DIR="$ROOT_DIR/packaging/homebrew"
BUILD_DIR="$ROOT_DIR/build/homebrew"
FORMULA_TEMPLATE="$PACKAGING_DIR/dotnetapidiff.rb"

# Create build directory
mkdir -p "$BUILD_DIR"

# Copy and update formula
cp "$FORMULA_TEMPLATE" "$BUILD_DIR/dotnetapidiff.rb"

# Update placeholders in the formula (macOS-compatible sed)
if [[ "$OSTYPE" == "darwin"* ]]; then
    # macOS sed
    sed -i '' \
        -e "s/\\\$version\\\$/$VERSION/g" \
        -e "s/\\\$sha256_osx_arm64\\\$/$SHA256_OSX_ARM64/g" \
        -e "s/\\\$sha256_osx_x64\\\$/$SHA256_OSX_X64/g" \
        -e "s/\\\$sha256_linux_arm64\\\$/$SHA256_LINUX_ARM64/g" \
        -e "s/\\\$sha256_linux_x64\\\$/$SHA256_LINUX_X64/g" \
        "$BUILD_DIR/dotnetapidiff.rb"
else
    # GNU sed
    sed -i \
        -e "s/\\\$version\\\$/$VERSION/g" \
        -e "s/\\\$sha256_osx_arm64\\\$/$SHA256_OSX_ARM64/g" \
        -e "s/\\\$sha256_osx_x64\\\$/$SHA256_OSX_X64/g" \
        -e "s/\\\$sha256_linux_arm64\\\$/$SHA256_LINUX_ARM64/g" \
        -e "s/\\\$sha256_linux_x64\\\$/$SHA256_LINUX_X64/g" \
        "$BUILD_DIR/dotnetapidiff.rb"
fi

echo "Homebrew formula updated successfully!"
echo "Formula saved to: $BUILD_DIR/dotnetapidiff.rb"

# Copy to artifacts directory
ARTIFACTS_DIR="$ROOT_DIR/artifacts"
mkdir -p "$ARTIFACTS_DIR"
cp "$BUILD_DIR/dotnetapidiff.rb" "$ARTIFACTS_DIR/"

echo "Formula copied to artifacts directory"

# Validate the formula syntax (if brew is available)
if command -v brew >/dev/null 2>&1; then
    echo "Validating formula syntax..."
    if brew audit --formula "$BUILD_DIR/dotnetapidiff.rb"; then
        echo "Formula validation passed!"
    else
        echo "Warning: Formula validation failed"
        exit 1
    fi
else
    echo "Homebrew not available - skipping formula validation"
fi

echo "Homebrew formula update completed successfully!"

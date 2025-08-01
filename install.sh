#!/usr/bin/env bash

# DotNetApiDiff Universal Installation Script
#
# This script automatically downloads and installs the appropriate DotNetApiDiff
# binary from GitHub Releases for Linux and macOS systems.
#
# Usage:
#   curl -fsSL https://raw.githubusercontent.com/jbrinkman/dotnet-api-diff/main/install.sh | bash
#   curl -fsSL https://raw.githubusercontent.com/jbrinkman/dotnet-api-diff/main/install.sh | bash -s -- v1.2.3
#   curl -fsSL https://raw.githubusercontent.com/jbrinkman/dotnet-api-diff/main/install.sh | sudo bash -s -- --system
#

set -euo pipefail

# Constants
readonly GITHUB_REPO="jbrinkman/dotnet-api-diff"
readonly BINARY_NAME="DotNetApiDiff"
readonly TEMP_DIR=$(mktemp -d)
readonly SCRIPT_NAME="DotNetApiDiff Installer"

# Default values
INSTALL_VERSION="latest"
INSTALL_PREFIX=""
SYSTEM_INSTALL=false
FORCE_INSTALL=false
VERIFY_CHECKSUM=true
UPDATE_PATH=true
QUIET=false

# Colors for output
readonly RED='\033[0;31m'
readonly GREEN='\033[0;32m'
readonly YELLOW='\033[1;33m'
readonly BLUE='\033[0;34m'
readonly CYAN='\033[0;36m'
readonly NC='\033[0m' # No Color

# Cleanup function
cleanup() {
    if [[ -d "$TEMP_DIR" ]]; then
        rm -rf "$TEMP_DIR"
    fi
}

# Trap cleanup on exit
trap cleanup EXIT

# Print functions
print_header() {
    if [[ "$QUIET" != "true" ]]; then
        echo -e "${CYAN}$SCRIPT_NAME${NC}"
        echo -e "${CYAN}========================${NC}"
    fi
}

print_info() {
    if [[ "$QUIET" != "true" ]]; then
        echo -e "${BLUE}ℹ${NC} $1"
    fi
}

print_success() {
    if [[ "$QUIET" != "true" ]]; then
        echo -e "${GREEN}✓${NC} $1"
    fi
}

print_warning() {
    if [[ "$QUIET" != "true" ]]; then
        echo -e "${YELLOW}⚠${NC} $1"
    fi
}

print_error() {
    echo -e "${RED}✗${NC} $1" >&2
}

# Error handling
die() {
    print_error "$1"
    exit 1
}

# Help function
show_help() {
    cat << EOF
$SCRIPT_NAME

Download and install DotNetApiDiff from GitHub Releases.

USAGE:
    install.sh [OPTIONS] [VERSION]

OPTIONS:
    -h, --help              Show this help message
    -v, --version <version> Install specific version (default: latest)
    -p, --prefix <path>     Install to custom directory
    -s, --system            Install system-wide (requires sudo)
    -f, --force             Overwrite existing installation
    -q, --quiet             Suppress non-error output
    --no-checksum           Skip checksum verification (not recommended)
    --no-path-update        Don't modify PATH automatically

EXAMPLES:
    # Install latest version to user directory
    install.sh

    # Install specific version
    install.sh --version v1.2.3
    install.sh v1.2.3

    # Install system-wide (requires sudo)
    sudo install.sh --system

    # Install to custom directory
    install.sh --prefix /opt/mytools

    # Download and run via curl
    curl -fsSL https://raw.githubusercontent.com/jbrinkman/dotnet-api-diff/main/install.sh | bash
    curl -fsSL https://raw.githubusercontent.com/jbrinkman/dotnet-api-diff/main/install.sh | bash -s -- v1.2.3

INSTALLATION LOCATIONS:
    User Installation (default):
        ~/.local/bin/           (preferred)
        ~/bin/                  (fallback)

    System Installation (--system flag):
        /usr/local/bin/         (preferred)
        /opt/dotnetapidiff/bin/ (fallback)

EOF
}

# Parse command line arguments
parse_args() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            -h|--help)
                show_help
                exit 0
                ;;
            -v|--version)
                INSTALL_VERSION="$2"
                shift 2
                ;;
            -p|--prefix)
                INSTALL_PREFIX="$2"
                shift 2
                ;;
            -s|--system)
                SYSTEM_INSTALL=true
                shift
                ;;
            -f|--force)
                FORCE_INSTALL=true
                shift
                ;;
            -q|--quiet)
                QUIET=true
                shift
                ;;
            --no-checksum)
                VERIFY_CHECKSUM=false
                shift
                ;;
            --no-path-update)
                UPDATE_PATH=false
                shift
                ;;
            -*)
                die "Unknown option: $1. Use --help for usage information."
                ;;
            *)
                # Assume it's a version if no prefix
                if [[ -z "$INSTALL_PREFIX" && "$INSTALL_VERSION" == "latest" ]]; then
                    INSTALL_VERSION="$1"
                else
                    die "Unexpected argument: $1. Use --help for usage information."
                fi
                shift
                ;;
        esac
    done
}

# Detect operating system
detect_os() {
    local os
    os=$(uname -s | tr '[:upper:]' '[:lower:]')

    case $os in
        linux*)
            echo "linux"
            ;;
        darwin*)
            echo "osx"
            ;;
        *)
            die "Unsupported operating system: $os. Only Linux and macOS are supported."
            ;;
    esac
}

# Detect CPU architecture
detect_arch() {
    local arch
    arch=$(uname -m)

    case $arch in
        x86_64|amd64)
            echo "x64"
            ;;
        aarch64|arm64)
            echo "arm64"
            ;;
        *)
            die "Unsupported architecture: $arch. Only x64 and ARM64 are supported."
            ;;
    esac
}

# Check if command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Ensure required tools are available
check_dependencies() {
    local missing_tools=()

    if ! command_exists curl; then
        missing_tools+=("curl")
    fi

    if ! command_exists tar && [[ "$OS" != "win" ]]; then
        missing_tools+=("tar")
    fi

    if [[ "$VERIFY_CHECKSUM" == "true" ]] && ! command_exists sha256sum && ! command_exists shasum; then
        print_warning "Neither sha256sum nor shasum found. Checksum verification will be disabled."
        VERIFY_CHECKSUM=false
    fi

    if [[ ${#missing_tools[@]} -gt 0 ]]; then
        die "Missing required tools: ${missing_tools[*]}. Please install them and try again."
    fi
}

# Get latest release version from GitHub API
get_latest_version() {
    print_info "Fetching latest release information..." >&2

    local latest_info
    latest_info=$(curl -fsSL "https://api.github.com/repos/$GITHUB_REPO/releases/latest" 2>/dev/null) || {
        die "Failed to fetch latest release information. Please check your internet connection."
    }

    local version
    version=$(echo "$latest_info" | grep '"tag_name"' | sed -E 's/.*"tag_name": "([^"]+)".*/\1/')

    if [[ -z "$version" ]]; then
        die "Failed to parse latest version from GitHub API response."
    fi

    echo "$version"
}

# Validate that a specific version exists
validate_version() {
    local version="$1"
    print_info "Validating version $version..." >&2

    local release_info
    release_info=$(curl -fsSL "https://api.github.com/repos/$GITHUB_REPO/releases/tags/$version" 2>/dev/null) || {
        die "Version $version not found. Please check the version and try again."
    }

    if echo "$release_info" | grep -q '"message": "Not Found"'; then
        die "Version $version not found in releases."
    fi
}

# Get asset information for a specific version
get_asset_info() {
    local version="$1"
    local os="$2"
    local arch="$3"

    print_info "Getting download information for $os-$arch..." >&2

    # Determine file extension based on OS
    local file_ext
    case $os in
        linux|osx)
            file_ext="tar.gz"
            ;;
        win)
            file_ext="zip"
            ;;
        *)
            die "Unsupported OS: $os"
            ;;
    esac

    local asset_name="dotnet-api-diff-$os-$arch.$file_ext"
    local release_info
    release_info=$(curl -fsSL "https://api.github.com/repos/$GITHUB_REPO/releases/tags/$version" 2>/dev/null) || {
        die "Failed to fetch release information for version $version."
    }

    # Extract download URL and checksum
    local download_url
    download_url=$(echo "$release_info" | grep -A 30 "\"name\": \"$asset_name\"" | grep '"browser_download_url"' | sed -E 's/.*"browser_download_url": "([^"]+)".*/\1/')

    local checksum
    checksum=$(echo "$release_info" | grep -A 30 "\"name\": \"$asset_name\"" | grep '"digest"' | sed -E 's/.*"sha256:([^"]+)".*/\1/')

    # If ARM64 build not found, fall back to x64
    if [[ -z "$download_url" && "$arch" == "arm64" ]]; then
        print_warning "ARM64 build not available for $version, falling back to x64" >&2
        asset_name="dotnet-api-diff-$os-x64.$file_ext"
        download_url=$(echo "$release_info" | grep -A 30 "\"name\": \"$asset_name\"" | grep '"browser_download_url"' | sed -E 's/.*"browser_download_url": "([^"]+)".*/\1/')
        checksum=$(echo "$release_info" | grep -A 30 "\"name\": \"$asset_name\"" | grep '"digest"' | sed -E 's/.*"sha256:([^"]+)".*/\1/')
    fi

    if [[ -z "$download_url" ]]; then
        if [[ "$arch" == "arm64" ]]; then
            die "Binary for $os-$arch not found in release $version, and fallback to x64 also failed."
        else
            die "Binary for $os-$arch not found in release $version."
        fi
    fi

    echo "$download_url|$checksum"
}

# Download file with progress
download_file() {
    local url="$1"
    local output_file="$2"

    print_info "Downloading $(basename "$output_file")..."

    if [[ "$QUIET" == "true" ]]; then
        curl -fsSL -o "$output_file" "$url" || die "Failed to download file from $url"
    else
        curl -fSL --progress-bar -o "$output_file" "$url" || die "Failed to download file from $url"
    fi
}

# Verify file checksum
verify_checksum() {
    local file="$1"
    local expected_checksum="$2"

    if [[ "$VERIFY_CHECKSUM" != "true" ]] || [[ -z "$expected_checksum" ]]; then
        return 0
    fi

    print_info "Verifying file integrity..."

    local actual_checksum
    if command_exists sha256sum; then
        actual_checksum=$(sha256sum "$file" | cut -d' ' -f1)
    elif command_exists shasum; then
        actual_checksum=$(shasum -a 256 "$file" | cut -d' ' -f1)
    else
        print_warning "No checksum tool available. Skipping verification."
        return 0
    fi

    if [[ "$actual_checksum" != "$expected_checksum" ]]; then
        die "Checksum verification failed. Expected: $expected_checksum, Got: $actual_checksum"
    fi

    print_success "Checksum verified"
}

# Extract downloaded archive
extract_archive() {
    local archive_file="$1"
    local extract_dir="$2"

    print_info "Extracting archive..."

    case "$archive_file" in
        *.tar.gz)
            tar -xzf "$archive_file" -C "$extract_dir" || die "Failed to extract tar.gz archive"
            ;;
        *.zip)
            if command_exists unzip; then
                unzip -q "$archive_file" -d "$extract_dir" || die "Failed to extract zip archive"
            else
                die "unzip command not found. Cannot extract .zip files."
            fi
            ;;
        *)
            die "Unsupported archive format: $archive_file"
            ;;
    esac

    print_success "Archive extracted"
}

# Determine installation directory
get_install_dir() {
    if [[ -n "$INSTALL_PREFIX" ]]; then
        echo "$INSTALL_PREFIX"
        return
    fi

    if [[ "$SYSTEM_INSTALL" == "true" ]]; then
        # System installation - requires elevated privileges
        if [[ -w "/usr/local/bin" ]] || [[ "$EUID" -eq 0 ]]; then
            echo "/usr/local/bin"
        else
            echo "/opt/dotnetapidiff/bin"
        fi
    else
        # User installation
        if [[ -d "$HOME/.local/bin" ]] || mkdir -p "$HOME/.local/bin" 2>/dev/null; then
            echo "$HOME/.local/bin"
        else
            echo "$HOME/bin"
        fi
    fi
}

# Check if installation directory is writable
check_install_permissions() {
    local install_dir="$1"

    # Create directory if it doesn't exist
    if [[ ! -d "$install_dir" ]]; then
        if ! mkdir -p "$install_dir" 2>/dev/null; then
            if [[ "$SYSTEM_INSTALL" == "true" ]]; then
                die "Cannot create directory $install_dir. Try running with sudo or use --prefix to specify a different location."
            else
                die "Cannot create directory $install_dir. Check permissions or use --prefix to specify a different location."
            fi
        fi
    fi

    # Check if directory is writable
    if [[ ! -w "$install_dir" ]]; then
        if [[ "$SYSTEM_INSTALL" == "true" ]]; then
            die "Cannot write to $install_dir. Try running with sudo."
        else
            die "Cannot write to $install_dir. Check permissions or use --prefix to specify a different location."
        fi
    fi
}

# Install binary to target directory
install_binary() {
    local extract_dir="$1"
    local install_dir="$2"

    print_info "Installing to $install_dir..."

    # Find the binary in the extracted files
    local binary_path
    binary_path=$(find "$extract_dir" -name "$BINARY_NAME" -type f | head -1)

    if [[ -z "$binary_path" ]]; then
        die "Binary '$BINARY_NAME' not found in extracted files."
    fi

    # Install with user-friendly name
    local target_name="dotnetapidiff"
    local target_path="$install_dir/$target_name"

    # Check if binary already exists
    if [[ -f "$target_path" ]] && [[ "$FORCE_INSTALL" != "true" ]]; then
        print_warning "Binary already exists at $target_path. Use --force to overwrite."
        local existing_version
        existing_version=$("$target_path" --version 2>/dev/null | head -1 || echo "unknown")
        print_info "Existing version: $existing_version"
        return 1
    fi

    # Copy binary and set permissions
    cp "$binary_path" "$target_path" || die "Failed to copy binary to $target_path"
    chmod +x "$target_path" || die "Failed to set executable permissions on $target_path"

    print_success "Binary installed to $target_path"
}

# Check if directory is in PATH
is_in_path() {
    local dir="$1"
    case ":$PATH:" in
        *":$dir:"*) return 0 ;;
        *) return 1 ;;
    esac
}

# Update PATH if necessary
update_path() {
    local install_dir="$1"

    if [[ "$UPDATE_PATH" != "true" ]]; then
        return 0
    fi

    if is_in_path "$install_dir"; then
        print_success "$install_dir is already in PATH"
        return 0
    fi

    print_info "Adding $install_dir to PATH..."

    # Determine which shell config file to update
    local shell_config=""
    local shell_name
    shell_name=$(basename "$SHELL")

    case $shell_name in
        bash)
            if [[ -f "$HOME/.bashrc" ]]; then
                shell_config="$HOME/.bashrc"
            elif [[ -f "$HOME/.bash_profile" ]]; then
                shell_config="$HOME/.bash_profile"
            fi
            ;;
        zsh)
            shell_config="$HOME/.zshrc"
            ;;
        fish)
            shell_config="$HOME/.config/fish/config.fish"
            ;;
    esac

    if [[ -n "$shell_config" ]]; then
        # Add PATH export to shell config
        local path_export="export PATH=\"$install_dir:\$PATH\""

        if ! grep -q "$install_dir" "$shell_config" 2>/dev/null; then
            echo "" >> "$shell_config"
            echo "# Added by DotNetApiDiff installer" >> "$shell_config"
            echo "$path_export" >> "$shell_config"
            print_success "Added $install_dir to PATH in $shell_config"
            print_info "Please restart your shell or run: source $shell_config"
        else
            print_success "$install_dir already in $shell_config"
        fi
    else
        print_warning "Could not automatically update PATH. Please add $install_dir to your PATH manually."
    fi
}

# Verify installation
verify_installation() {
    local install_dir="$1"
    local version="$2"

    print_info "Verifying installation..."

    local binary_path="$install_dir/dotnetapidiff"

    if [[ ! -f "$binary_path" ]]; then
        die "Installation verification failed: binary not found at $binary_path"
    fi

    if [[ ! -x "$binary_path" ]]; then
        die "Installation verification failed: binary is not executable"
    fi

    # Test that the binary can run
    local installed_version
    if installed_version=$("$binary_path" --version 2>/dev/null | head -1); then
        print_success "Installation verified: $installed_version"
    else
        print_warning "Binary installed but version check failed"
    fi
}

# Main installation function
main() {
    print_header

    # Parse command line arguments
    parse_args "$@"

    # Check dependencies
    check_dependencies

    # Detect system information
    local os arch
    os=$(detect_os)
    arch=$(detect_arch)
    print_info "Detected: $(uname -s) $arch"

    # Determine version to install
    local version="$INSTALL_VERSION"
    if [[ "$version" == "latest" ]]; then
        version=$(get_latest_version)
    else
        validate_version "$version"
    fi
    print_info "Version: $version"

    # Get download information
    local asset_info download_url checksum
    asset_info=$(get_asset_info "$version" "$os" "$arch")
    download_url=$(echo "$asset_info" | cut -d'|' -f1)
    checksum=$(echo "$asset_info" | cut -d'|' -f2)

    # Determine installation directory
    local install_dir
    install_dir=$(get_install_dir)
    print_info "Install directory: $install_dir"

    # Check permissions
    check_install_permissions "$install_dir"

    # Download and extract
    local archive_file="$TEMP_DIR/$(basename "$download_url")"
    download_file "$download_url" "$archive_file"
    verify_checksum "$archive_file" "$checksum"

    local extract_dir="$TEMP_DIR/extract"
    mkdir -p "$extract_dir"
    extract_archive "$archive_file" "$extract_dir"

    # Install binary
    if install_binary "$extract_dir" "$install_dir"; then
        # Update PATH
        update_path "$install_dir"

        # Verify installation
        verify_installation "$install_dir" "$version"

        echo ""
        print_success "Installation complete!"
        print_info "Run 'dotnetapidiff --version' to verify installation."

        if ! is_in_path "$install_dir"; then
            echo ""
            print_warning "Note: $install_dir is not in your PATH."
            print_info "You may need to restart your shell or update your PATH manually."
        fi
    else
        die "Installation failed"
    fi
}

# Run main function with all arguments
main "$@"

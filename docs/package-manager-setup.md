# Package Manager Setup Guide

This document explains how to set up automated publishing to Chocolatey and Homebrew package managers for DotNet API Diff releases.

## Overview

The release workflow automatically builds and publishes packages to:
- **Chocolatey** (Windows package manager)
- **Homebrew** (macOS/Linux package manager)

## Prerequisites

### Repository Secrets

The following secrets must be configured in the GitHub repository settings:

1. **CHOCOLATEY_API_KEY**: API key for publishing to Chocolatey

### Package Manager Accounts

#### Chocolatey Setup

1. **Create Chocolatey Account**:
   - Visit [Chocolatey Community](https://community.chocolatey.org/)
   - Create an account or sign in
   - Navigate to your profile and generate an API key

2. **Package Registration**:
   - The first package version must be manually submitted for approval
   - Subsequent versions will be automatically published via the API
   - Package ID: `dotnetapidiff`

3. **Configure GitHub Secret**:
   ```bash
   # In GitHub repository settings > Secrets and variables > Actions
   # Add new repository secret:
   Name: CHOCOLATEY_API_KEY
   Value: [Your Chocolatey API key]
   ```

#### Homebrew Setup

Homebrew publishing is currently handled by generating an updated formula that can be:
1. **Submitted to homebrew-core** (requires community approval)
2. **Published to a custom tap** (immediate publishing)

For custom tap approach:
1. Create a repository named `homebrew-dotnetapidiff`
2. The workflow generates the updated formula as an artifact
3. Manually or automatically commit the formula to the tap repository

## Workflow Details

### Chocolatey Publishing

The workflow automatically:
1. Builds Windows x64 and ARM64 binaries
2. Calculates SHA256 checksums
3. Creates a Chocolatey package (.nupkg)
4. Publishes to Chocolatey Community using the API key

**Package Structure:**
```
dotnetapidiff/
├── tools/
│   ├── chocolateyinstall.ps1    # Installation script
│   ├── chocolateyuninstall.ps1  # Uninstallation script
│   └── dotnetapidiff.bat        # Command wrapper
└── dotnetapidiff.nuspec         # Package metadata
```

### Homebrew Formula

The workflow automatically:
1. Downloads all platform binaries (macOS/Linux, x64/ARM64)
2. Calculates SHA256 checksums for each platform
3. Generates an updated Homebrew formula with correct URLs and checksums
4. Uploads the formula as a build artifact

**Formula Features:**
- Platform detection (macOS/Linux, Intel/ARM)
- Automatic binary selection based on platform
- SHA256 verification
- Installs binary as `dotnetapidiff`

## Package Testing

### Chocolatey Testing

```powershell
# Install package
choco install dotnetapidiff

# Test installation
dotnetapidiff --version

# Uninstall
choco uninstall dotnetapidiff
```

### Homebrew Testing

```bash
# Install from custom tap (if using tap approach)
brew tap jbrinkman/dotnetapidiff
brew install dotnetapidiff

# Test installation
dotnetapidiff --version

# Uninstall
brew uninstall dotnetapidiff
```

## Manual Package Creation

For testing or manual publishing:

### Chocolatey

```powershell
# Build package locally
.\scripts\build-chocolatey.ps1 -Version "1.0.0" -ChecksumX64 "abc123..." -ChecksumArm64 "def456..."

# Test package locally
choco install .\build\chocolatey\dotnetapidiff.1.0.0.nupkg

# Publish manually
.\scripts\build-chocolatey.ps1 -Version "1.0.0" -ChecksumX64 "abc123..." -ChecksumArm64 "def456..." -ApiKey "your-api-key" -Publish
```

### Homebrew

```bash
# Generate formula
./scripts/update-homebrew.sh "1.0.0" "osx_arm64_hash" "osx_x64_hash" "linux_arm64_hash" "linux_x64_hash"

# Test formula (if brew is available)
brew install ./build/homebrew/dotnetapidiff.rb
```

## Troubleshooting

### Common Issues

1. **Chocolatey API Key Invalid**:
   - Verify the API key in repository secrets
   - Check if the key has expired or been revoked

2. **Package Approval Required**:
   - First-time packages on Chocolatey require manual approval
   - Check the package status on Chocolatey Community

3. **Checksum Mismatch**:
   - Ensure release assets are fully uploaded before package creation
   - Verify download URLs are accessible

4. **Homebrew Formula Validation Fails**:
   - Check formula syntax using `brew audit`
   - Ensure all download URLs are valid and accessible

### Debugging

1. **Check Workflow Logs**:
   - GitHub Actions logs show detailed error messages
   - Look for failed steps in package publishing jobs

2. **Test Locally**:
   - Run packaging scripts manually with test data
   - Verify package installation on target platforms

3. **Package Manager Status**:
   - Check package status on Chocolatey Community
   - Verify formula syntax and platform support

## Security Considerations

1. **API Key Management**:
   - Store API keys only in GitHub repository secrets
   - Regularly rotate API keys
   - Limit API key permissions to package publishing only

2. **Package Integrity**:
   - All packages include SHA256 checksums
   - Binaries are built from tagged releases only
   - Package contents are validated during build

3. **Automated Publishing**:
   - Only triggered on tagged releases (v*.*)
   - Requires successful build and test completion
   - Includes rollback strategies for failed publications

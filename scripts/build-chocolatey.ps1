#!/usr/bin/env pwsh

param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[a-fA-F0-9]{64}$')]
    [string]$ChecksumX64,

    [Parameter(Mandatory = $true)]
    [string]$ChecksumArm64,

    [Parameter(Mandatory = $false)]
    [string]$ApiKey,

    [Parameter(Mandatory = $false)]
    [switch]$Publish
)

$ErrorActionPreference = 'Stop'

Write-Host "Building Chocolatey package for version $Version"

# Paths
$rootDir = Split-Path -Parent $PSScriptRoot
$packagingDir = Join-Path $rootDir "packaging/chocolatey"
$buildDir = Join-Path $rootDir "build/chocolatey"
$nuspecPath = Join-Path $packagingDir "dotnetapidiff.nuspec"
$installScriptPath = Join-Path $packagingDir "tools/chocolateyinstall.ps1"

# Create build directory
if (Test-Path $buildDir) {
    Remove-Item $buildDir -Recurse -Force
}
New-Item -ItemType Directory -Path $buildDir -Force | Out-Null

# Copy package files to build directory
Copy-Item -Path "$packagingDir/*" -Destination $buildDir -Recurse -Force

# Update version in nuspec
$nuspecContent = Get-Content $nuspecPath -Raw
$nuspecContent = $nuspecContent.Replace('$version$', $Version)
$nuspecContent | Set-Content (Join-Path $buildDir "dotnetapidiff.nuspec")

# Update checksums in install script
$installScriptContent = Get-Content $installScriptPath -Raw
$installScriptContent = $installScriptContent -replace '\$checksum64\$', $ChecksumX64
$installScriptContent = $installScriptContent -replace '\$checksumArm64\$', $ChecksumArm64

$installScriptContent | Set-Content (Join-Path $buildDir "tools/chocolateyinstall.ps1")

# Build package
Push-Location $buildDir
try {
    $nupkgFile = "dotnetapidiff.$Version.nupkg"

    Write-Host "Creating Chocolatey package..."
    choco pack dotnetapidiff.nuspec

    if (-not (Test-Path $nupkgFile)) {
        throw "Package creation failed - $nupkgFile not found"
    }

    Write-Host "Package created successfully: $nupkgFile"

    if ($Publish) {
        if (-not $ApiKey) {
            throw "API key is required for publishing"
        }

        Write-Host "Publishing package to Chocolatey..."
        choco push $nupkgFile --api-key $ApiKey
        Write-Host "Package published successfully!"
    }
    else {
        Write-Host "Package built but not published (use -Publish flag to publish)"
    }

    # Copy package to artifacts directory
    $artifactsDir = Join-Path $rootDir "artifacts"
    if (-not (Test-Path $artifactsDir)) {
        New-Item -ItemType Directory -Path $artifactsDir -Force | Out-Null
    }
    Copy-Item $nupkgFile $artifactsDir -Force
    Write-Host "Package copied to artifacts directory"

}
finally {
    Pop-Location
}

Write-Host "Chocolatey packaging completed successfully!"

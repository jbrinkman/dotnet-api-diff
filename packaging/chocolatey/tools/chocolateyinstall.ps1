$ErrorActionPreference = 'Stop';

$packageName = 'dotnetapidiff'
$toolsDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$version = $env:ChocolateyPackageVersion

# Download URLs - these will be updated by the release workflow
$url64 = "https://github.com/jbrinkman/dotnet-api-diff/releases/download/v$version/dotnet-api-diff-win-x64.zip"
$urlArm64 = "https://github.com/jbrinkman/dotnet-api-diff/releases/download/v$version/dotnet-api-diff-win-arm64.zip"

# Checksums - these will be updated by the release workflow
$checksum64 = '$checksum64$'
$checksumArm64 = '$checksumArm64$'

$packageArgs = @{
    packageName    = $packageName
    unzipLocation  = $toolsDir
    fileType       = 'zip'
    url64bit       = $url64
    checksum64     = $checksum64
    checksumType64 = 'sha256'
}

# Check if we're on ARM64 and the ARM64 version is available
$isArm64 = $env:PROCESSOR_ARCHITECTURE -eq 'ARM64' -or $env:PROCESSOR_ARCHITEW6432 -eq 'ARM64'

if ($isArm64 -and $urlArm64 -and $checksumArm64 -ne '$checksumArm64$') {
    Write-Host "Detected ARM64 architecture, using ARM64 build"
    $packageArgs.url64bit = $urlArm64
    $packageArgs.checksum64 = $checksumArm64
}
else {
    Write-Host "Using x64 build (compatible with ARM64 via emulation)"
}

# Install the package
Install-ChocolateyZipPackage @packageArgs

# Create a shim for the executable
$exePath = Join-Path $toolsDir "DotNetApiDiff.exe"
if (Test-Path $exePath) {
    # Create a shim with a user-friendly name
    Install-ChocolateyPath $toolsDir

    # Create batch file for easier command line usage
    $batchPath = Join-Path $toolsDir "dotnetapidiff.bat"
    $batchContent = "@echo off`r`n`"$exePath`" %*"
    [System.IO.File]::WriteAllText($batchPath, $batchContent)

    Write-Host "DotNet API Diff has been installed successfully!"
    Write-Host "You can now use 'dotnetapidiff' from any command prompt."
    Write-Host "Type 'dotnetapidiff --help' to get started."
}
else {
    throw "Installation failed: DotNetApiDiff.exe not found in package"
}

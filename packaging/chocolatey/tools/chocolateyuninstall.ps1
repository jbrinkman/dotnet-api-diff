$ErrorActionPreference = 'Stop';

$packageName = 'dotnetapidiff'

# Remove the shim
Uninstall-ChocolateyZipPackage $packageName -zipFileName "*.zip"

# Clean up batch file if it exists
$toolsDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$batchPath = Join-Path $toolsDir "dotnetapidiff.bat"
if (Test-Path $batchPath) {
    Remove-Item $batchPath -Force
}

Write-Host "DotNet API Diff has been uninstalled successfully!"

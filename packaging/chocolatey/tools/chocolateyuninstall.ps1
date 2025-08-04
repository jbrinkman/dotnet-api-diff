$ErrorActionPreference = 'Stop';

$packageName = 'dotnetapidiff'

# Remove extracted files
$toolsDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
Get-ChildItem -Path $toolsDir -Include *.exe, *.dll, *.json, *.xml, *.pdb -File | ForEach-Object { Remove-Item $_.FullName -Force }

# Clean up batch file if it exists
$batchPath = Join-Path $toolsDir "dotnetapidiff.bat"
if (Test-Path $batchPath) {
    Remove-Item $batchPath -Force
}

Write-Host "DotNet API Diff has been uninstalled successfully!"

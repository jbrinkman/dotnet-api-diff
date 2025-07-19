# Fix-TrailingCommas.ps1
# Script to fix StyleCop SA1413 issues: Use trailing comma in multi-line initializers

function Repair-TrailingCommas {
    param (
        [string]$content
    )

    # Fix SA1413: Use trailing comma in multi-line initializers
    # Find multi-line initializers without trailing commas
    $pattern = "(\s+)([^,\s\r\n]+)(\s*)(\r?\n\s*})"
    $replacement = '$1$2,$3$4'
    $content = [regex]::Replace($content, $pattern, $replacement)

    return $content
}

# Process all C# files in the src directory
Get-ChildItem -Path "src" -Filter "*.cs" -Recurse | ForEach-Object {
    $file = $_.FullName
    Write-Host "Processing $file"

    # Read file content
    $content = Get-Content -Path $file -Raw

    # Fix trailing comma issues
    $content = Repair-TrailingCommas -content $content

    # Write the fixed content back to the file
    Set-Content -Path $file -Value $content -NoNewline

    # Ensure file ends with newline
    Add-Content -Path $file -Value "`n" -NoNewline

    Write-Host "Fixed trailing comma issues in $file"
}

Write-Host "Trailing comma fixes completed for all C# files in src directory"
Write-Host "Note: Some complex initializer patterns may require manual fixes."

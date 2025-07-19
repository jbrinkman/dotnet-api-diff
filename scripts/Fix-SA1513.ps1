# Fix-SA1513.ps1
# Script to fix StyleCop SA1513 issues: Closing brace should be followed by blank line

function Repair-ClosingBraces {
    param (
        [string]$content
    )

    # Replace closing braces that aren't followed by a blank line
    # This regex looks for a closing brace followed by a newline that isn't followed by another newline
    $content = $content -replace "}\r?\n(?!\r?\n)(?![ \t]*[}\)])", "}`r`n`r`n"

    return $content
}

# Process all C# files in the src directory
Get-ChildItem -Path "src" -Filter "*.cs" -Recurse | ForEach-Object {
    $file = $_.FullName
    Write-Host "Processing $file"

    # Read file content
    $content = Get-Content -Path $file -Raw

    # Fix SA1513 issues
    $content = Repair-ClosingBraces -content $content

    # Write the fixed content back to the file
    Set-Content -Path $file -Value $content -NoNewline

    # Ensure file ends with newline
    Add-Content -Path $file -Value "`n" -NoNewline

    Write-Host "Fixed StyleCop SA1513 issues in $file"
}

Write-Host "SA1513 fixes completed for all C# files in src directory"

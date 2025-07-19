# Fix-ParameterFormatting.ps1
# Script to fix StyleCop SA1116 and SA1117 issues: Parameter formatting

function Repair-ParameterFormatting {
    param (
        [string]$content
    )

    # Fix SA1116: Parameters should begin on line after declaration when spanning multiple lines
    # Find method calls with parameters spanning multiple lines but not starting on a new line
    $pattern1 = "(\w+\()([^\r\n]+)(\r?\n)"
    $replacement1 = '$1' + "`r`n        " + '$2$3'
    $content = [regex]::Replace($content, $pattern1, $replacement1)

    # Fix SA1117: Parameters should all be on same line or each on own line
    # This is more complex and might require manual intervention
    # This is a simplified approach that might not catch all cases
    $pattern2 = "(\w+\([^\r\n,]+,)([^\r\n,]+,)([^\r\n\)]+\))"
    $replacement2 = '$1' + "`r`n        " + '$2' + "`r`n        " + '$3'
    $content = [regex]::Replace($content, $pattern2, $replacement2)

    return $content
}

# Process all C# files in the src directory
Get-ChildItem -Path "src" -Filter "*.cs" -Recurse | ForEach-Object {
    $file = $_.FullName
    Write-Host "Processing $file"

    # Read file content
    $content = Get-Content -Path $file -Raw

    # Fix parameter formatting issues
    $content = Repair-ParameterFormatting -content $content

    # Write the fixed content back to the file
    Set-Content -Path $file -Value $content -NoNewline

    # Ensure file ends with newline
    Add-Content -Path $file -Value "`n" -NoNewline

    Write-Host "Fixed parameter formatting issues in $file"
}

Write-Host "Parameter formatting fixes completed for all C# files in src directory"
Write-Host "Note: Some complex parameter formatting issues may require manual fixes."

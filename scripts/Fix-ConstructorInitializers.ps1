# Fix-ConstructorInitializers.ps1
# Script to fix StyleCop SA1128 issues: Put constructor initializers on their own line

function Repair-ConstructorInitializers {
    param (
        [string]$content
    )

    # Replace constructor initializers that are on the same line
    # Find patterns like "public ClassName(params) : base(params)" and put the initializer on its own line
    $pattern = "(\s+public\s+\w+\([^)]*\))\s*:\s*([^\r\n]+)"
    $replacement = '$1' + "`r`n        : `$2"
    $content = [regex]::Replace($content, $pattern, $replacement)

    return $content
}

# Process all C# files in the src directory
Get-ChildItem -Path "src" -Filter "*.cs" -Recurse | ForEach-Object {
    $file = $_.FullName
    Write-Host "Processing $file"

    # Read file content
    $content = Get-Content -Path $file -Raw

    # Fix SA1128 issues
    $content = Repair-ConstructorInitializers -content $content

    # Write the fixed content back to the file
    Set-Content -Path $file -Value $content -NoNewline

    # Ensure file ends with newline
    Add-Content -Path $file -Value "`n" -NoNewline

    Write-Host "Fixed constructor initializer issues in $file"
}

Write-Host "Constructor initializer fixes completed for all C# files in src directory"
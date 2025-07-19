# Add-FinalNewlines.ps1
# Script to ensure all C# files end with a newline

# Process all C# files in the src directory
Get-ChildItem -Path "src" -Filter "*.cs" -Recurse | ForEach-Object {
    $file = $_.FullName
    Write-Host "Processing $file"

    # Read file content
    $content = Get-Content -Path $file -Raw

    # Check if the file already ends with a newline
    if (-not $content.EndsWith("`n")) {
        # Add a newline to the end of the file
        Add-Content -Path $file -Value "`n" -NoNewline
        Write-Host "Added final newline to $file"
    }
    else {
        Write-Host "File $file already ends with a newline"
    }
}

Write-Host "Final newline fixes completed for all C# files in src directory"

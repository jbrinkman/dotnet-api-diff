# Find all C# files and ensure they have a final newline
Get-ChildItem -Path "." -Filter "*.cs" -Recurse | ForEach-Object {
    $file = $_.FullName
    $content = Get-Content -Path $file -Raw

    # Check if the file ends with a newline
    if (-not $content.EndsWith("`n")) {
        # Add a newline to the end of the file
        Add-Content -Path $file -Value ""
        Write-Host "Added final newline to $file"
    }
}

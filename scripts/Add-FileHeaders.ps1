# Define the header
$header = "// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT"

# Process files in the src directory
Get-ChildItem -Path "src" -Filter "*.cs" -Recurse | ForEach-Object {
    $file = $_.FullName
    $content = Get-Content -Path $file -Raw

    # Check if the file already has the header
    if (-not $content.Contains("SPDX Identifier: MIT")) {
        # Add the header to the beginning of the file
        $newContent = "$header`n$content"
        Set-Content -Path $file -Value $newContent
        Write-Host "Added header to $file"
    }
}

# Process files in the tests directory
Get-ChildItem -Path "tests" -Filter "*.cs" -Recurse | ForEach-Object {
    $file = $_.FullName
    $content = Get-Content -Path $file -Raw

    # Check if the file already has the header
    if (-not $content.Contains("SPDX Identifier: MIT")) {
        # Add the header to the beginning of the file
        $newContent = "$header`n$content"
        Set-Content -Path $file -Value $newContent
        Write-Host "Added header to $file"
    }
}

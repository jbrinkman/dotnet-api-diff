# Find-StyleCopIssues.ps1
# Script to identify files with specific StyleCop issues

# Define the StyleCop issues to look for
$styleCopIssues = @{
    "SA1201" = "A property should not follow a method"
    "SA1204" = "Static members should appear before non-static members"
    "SA1202" = "'public' members should come before 'private' members"
    "SA1128" = "Put constructor initializers on their own line"
    "SA1413" = "Use trailing comma in multi-line initializers"
    "SA1116" = "Parameters should begin on line after declaration when spanning multiple lines"
    "SA1117" = "Parameters should all be placed on the same line or each parameter should be placed on its own line"
}

# Process all C# files in the src directory
$files = Get-ChildItem -Path "src" -Filter "*.cs" -Recurse

# Create a report of files with potential issues
$report = @{}

foreach ($file in $files) {
    $content = Get-Content -Path $file.FullName -Raw
    $issues = @()

    # Check for SA1201: A property should not follow a method
    if ($content -match "public\s+\w+\s+\w+\([^)]*\)[^{]*\{[^}]*\}\s*public\s+\w+\s+\w+\s*\{") {
        $issues += "SA1201"
    }

    # Check for SA1204: Static members should appear before non-static members
    if ($content -match "public\s+\w+\s+\w+[^{]*\{[^}]*\}\s*public\s+static\s+\w+\s+\w+") {
        $issues += "SA1204"
    }

    # Check for SA1202: 'public' members should come before 'private' members
    if ($content -match "private\s+\w+\s+\w+[^{]*\{[^}]*\}\s*public\s+\w+\s+\w+") {
        $issues += "SA1202"
    }

    # Check for SA1128: Put constructor initializers on their own line
    if ($content -match "public\s+\w+\([^)]*\)\s*:\s+\w+\(") {
        $issues += "SA1128"
    }

    # Check for SA1413: Use trailing comma in multi-line initializers
    if ($content -match "\{\s*\w+[^,\n]*\n[^,\n]*\n[^,\n]*\}") {
        $issues += "SA1413"
    }

    # Check for SA1116: Parameters should begin on line after declaration when spanning multiple lines
    if ($content -match "\w+\([^)\n]+\n[^)]+\)") {
        $issues += "SA1116"
    }

    # Check for SA1117: Parameters should all be on same line or each on own line
    if ($content -match "\w+\([^,\n]+,\s*\n[^,\n]+,[^,\n]+\)") {
        $issues += "SA1117"
    }

    if ($issues.Count -gt 0) {
        $report[$file.FullName] = $issues
    }
}

# Output the report
Write-Host "Files with potential StyleCop issues:"
foreach ($file in $report.Keys) {
    Write-Host "`n$file"
    foreach ($issue in $report[$file]) {
        Write-Host "  - $issue: $($styleCopIssues[$issue])"
    }
}

Write-Host "`nNote: This script uses simple pattern matching and may produce false positives or miss some issues."
Write-Host "Manual review is recommended for the identified files."

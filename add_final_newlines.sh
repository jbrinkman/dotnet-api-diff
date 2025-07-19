#!/bin/bash

# Find all C# files and ensure they have a final newline
find . -name "*.cs" -type f -exec sh -c 'if [ "$(tail -c 1 "$1" | wc -l)" -eq 0 ]; then echo "" >> "$1"; fi' sh {} \;

# Fix the whitespace issue in AssemblyLoader.cs manually

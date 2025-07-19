#!/bin/bash

# Define the header
HEADER="// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT"

# Find all C# files in the src directory
find src -name "*.cs" | while read file; do
    # Check if the file already has the header
    if ! grep -q "SPDX Identifier: MIT" "$file"; then
        # Add the header to the beginning of the file
        echo "$HEADER" > temp_file
        cat "$file" >> temp_file
        mv temp_file "$file"
        echo "Added header to $file"
    fi
done

# Find all C# files in the tests directory
find tests -name "*.cs" | while read file; do
    # Check if the file already has the header
    if ! grep -q "SPDX Identifier: MIT" "$file"; then
        # Add the header to the beginning of the file
        echo "$HEADER" > temp_file
        cat "$file" >> temp_file
        mv temp_file "$file"
        echo "Added header to $file"
    fi
done

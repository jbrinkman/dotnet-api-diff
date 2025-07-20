# DotNet API Diff Tool

[![Main Branch Build](https://github.com/jbrinkman/dotnet-api-diff/actions/workflows/main-build.yml/badge.svg)](https://github.com/jbrinkman/dotnet-api-diff/actions/workflows/main-build.yml)
[![PR Build and Test](https://github.com/jbrinkman/dotnet-api-diff/actions/workflows/pr-build.yml/badge.svg)](https://github.com/jbrinkman/dotnet-api-diff/actions/workflows/pr-build.yml)

A command-line tool for comparing public APIs between different versions of .NET assemblies to detect breaking changes and API evolution.

## Features

- Compare public APIs between two .NET assembly versions
- Detect breaking changes, additions, removals, and modifications
- Generate reports in multiple formats (Console, JSON, XML, HTML, Markdown)
- Comprehensive analysis of types, methods, properties, fields, and events
- Severity classification for different types of changes

## Requirements

- .NET 8.0 SDK or later
- Compatible with .NET Framework, .NET Core, and .NET 5+ assemblies

## Building

```bash
# Clone the repository
git clone <repository-url>
cd DotNetApiDiff

# Build the solution
dotnet build

# Run tests
dotnet test
```

## Using Taskfile

This project uses [Taskfile](https://taskfile.dev/) to simplify common development tasks. Make sure you have Task installed:

```bash
# macOS
brew install go-task

# Linux
sh -c "$(curl --location https://taskfile.dev/install.sh)" -- -d -b ~/.local/bin

# Windows (with Chocolatey)
choco install go-task
```

### Common Tasks

```bash
# List all available tasks
task

# Build the solution
task build

# Run all tests
task test

# Generate code coverage report
task coverage

# View coverage report in browser
task coverage:view

# Run the application with arguments
task run -- compare source.dll target.dll

# Run CI build sequence
task ci
```

## Usage

```bash
# Basic comparison
dotnet run -- compare source.dll target.dll

# Generate JSON report
dotnet run -- compare source.dll target.dll --output json

# Generate markdown report
dotnet run -- compare source.dll target.dll --output markdown

# Filter to specific namespaces
dotnet run -- compare source.dll target.dll --filter System.Collections

# Filter to specific type patterns
dotnet run -- compare source.dll target.dll --type "System.Collections.*"

# Exclude specific namespaces or types
dotnet run -- compare source.dll target.dll --exclude Internal --exclude "System.Diagnostics.*"

# Include internal types in comparison
dotnet run -- compare source.dll target.dll --include-internals

# Include compiler-generated types in comparison
dotnet run -- compare source.dll target.dll --include-compiler-generated

# Use configuration file
dotnet run -- compare source.dll target.dll --config config.json

# Enable verbose output
dotnet run -- compare source.dll target.dll --verbose
```

## Configuration File

You can use a JSON configuration file to customize the comparison behavior. The configuration file supports the following sections:

### Sample Configuration File

```json
{
  "mappings": {
    "namespaceMappings": {
      "OldNamespace": ["NewNamespace"],
      "Legacy.Api": ["Modern.Api", "Modern.Api.V2"]
    },
    "typeMappings": {
      "OldNamespace.OldType": "NewNamespace.NewType"
    },
    "autoMapSameNameTypes": true,
    "ignoreCase": true
  },
  "exclusions": {
    "excludedTypes": ["System.Diagnostics.Debug"],
    "excludedTypePatterns": ["*.Internal.*", "*.Private.*"],
    "excludedMembers": ["System.Object.Finalize"],
    "excludedMemberPatterns": ["*.Obsolete*"]
  },
  "breakingChangeRules": {
    "treatTypeRemovalAsBreaking": true,
    "treatMemberRemovalAsBreaking": true,
    "treatAddedTypeAsBreaking": false,
    "treatAddedMemberAsBreaking": false,
    "treatSignatureChangeAsBreaking": true
  },
  "filters": {
    "includeNamespaces": ["System.Text", "System.IO"],
    "excludeNamespaces": ["System.Diagnostics"],
    "includeTypes": ["System.Text.*"],
    "excludeTypes": ["*.Internal*"],
    "includeInternals": false,
    "includeCompilerGenerated": false
  },
  "outputFormat": "Console",
  "outputPath": null,
  "failOnBreakingChanges": true
}
```

### Configuration Sections

- **mappings**: Define namespace and type name mappings between assemblies
  - **namespaceMappings**: Map source namespaces to target namespaces
  - **typeMappings**: Map specific source types to target types
  - **autoMapSameNameTypes**: Automatically map types with the same name but different namespaces
  - **ignoreCase**: Ignore case when comparing type and namespace names

- **exclusions**: Specify types and members to exclude from comparison
  - **excludedTypes**: List of fully qualified type names to exclude
  - **excludedTypePatterns**: Wildcard patterns for excluding types
  - **excludedMembers**: List of fully qualified member names to exclude
  - **excludedMemberPatterns**: Wildcard patterns for excluding members

- **breakingChangeRules**: Configure what changes are considered breaking
  - **treatTypeRemovalAsBreaking**: Whether removing a type is a breaking change
  - **treatMemberRemovalAsBreaking**: Whether removing a member is a breaking change
  - **treatAddedTypeAsBreaking**: Whether adding a type is a breaking change
  - **treatAddedMemberAsBreaking**: Whether adding a member is a breaking change
  - **treatSignatureChangeAsBreaking**: Whether changing a member signature is a breaking change

- **filters**: Control which types and namespaces are included in the comparison
  - **includeNamespaces**: List of namespaces to include (if empty, all namespaces are included)
  - **excludeNamespaces**: List of namespaces to exclude
  - **includeTypes**: List of type patterns to include
  - **excludeTypes**: List of type patterns to exclude
  - **includeInternals**: Whether to include internal types
  - **includeCompilerGenerated**: Whether to include compiler-generated types

- **outputFormat**: Format for the comparison report (Console, Json, Markdown)
- **outputPath**: Path to save the report (if null, output to console)
- **failOnBreakingChanges**: Whether to return a non-zero exit code if breaking changes are found

## Command Line Options

### Compare Command

- `<sourceAssembly>`: Path to the source/baseline assembly (required)
- `<targetAssembly>`: Path to the target/current assembly (required)
- `--config, -c`: Path to configuration file
- `--output, -o`: Output format (console, json, markdown)
- `--filter, -f`: Filter to specific namespaces (can be specified multiple times)
- `--type, -t`: Filter to specific type patterns (can be specified multiple times)
- `--exclude, -e`: Exclude types matching pattern (can be specified multiple times)
- `--include-internals`: Include internal types in the comparison
- `--include-compiler-generated`: Include compiler-generated types in the comparison
- `--no-color`: Disable colored output
- `--verbose, -v`: Enable verbose output
- `--help, -h`: Show help information

## Project Structure

```text
src/
├── DotNetApiDiff/
│   ├── Interfaces/          # Core service interfaces
│   ├── Models/              # Data models and enums
│   ├── Services/            # Implementation classes (to be added)
│   └── Program.cs           # Main entry point
tests/
└── DotNetApiDiff.Tests/     # Unit tests
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Ensure all tests pass
6. Submit a pull request

## License

This project is licensed under the MIT License.

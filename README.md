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
task run -- --old old.dll --new new.dll

# Run CI build sequence
task ci
```

## Usage

```bash
# Basic comparison
dotnet run -- --old path/to/old/assembly.dll --new path/to/new/assembly.dll

# Generate JSON report
dotnet run -- --old old.dll --new new.dll --format json --output report.json

# Show only breaking changes
dotnet run -- --old old.dll --new new.dll --breaking-only
```

## Command Line Options

- `--old, -o`: Path to the original assembly
- `--new, -n`: Path to the new assembly to compare against
- `--format, -f`: Output format (console, json, xml, html, markdown)
- `--output, -out`: Output file path (optional, defaults to console)
- `--breaking-only, -b`: Show only breaking changes
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

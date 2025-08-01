# DotNet API Diff Tool

[![Main Branch Build](https://github.com/jbrinkman/dotnet-api-diff/actions/workflows/main-build.yml/badge.svg)](https://github.com/jbrinkman/dotnet-api-diff/actions/workflows/main-build.yml)
[![PR Build and Test](https://github.com/jbrinkman/dotnet-api-diff/actions/workflows/pr-build.yml/badge.svg)](https://github.com/jbrinkman/dotnet-api-diff/actions/workflows/pr-build.yml)
[![Code Coverage](https://github.com/jbrinkman/dotnet-api-diff/actions/workflows/code-coverage.yml/badge.svg)](https://joe.brinkman.me/dotnet-api-diff)

A powerful command-line tool for comparing public APIs between different versions of .NET assemblies. Designed to help library maintainers and development teams identify breaking changes, track API evolution, and enforce semantic versioning practices.

## 🚀 Key Features

- **Comprehensive API Analysis**: Analyzes types, methods, properties, fields, events, and their signatures
- **Breaking Change Detection**: Automatically identifies and classifies breaking changes vs. non-breaking additions
- **Multiple Output Formats**: Generate reports in Console, JSON, XML, HTML, and Markdown formats
- **Flexible Filtering**: Include/exclude specific namespaces, types, or members using patterns
- **Configuration-Driven**: Use JSON configuration files for complex comparison scenarios
- **CI/CD Integration**: Perfect for automated builds and release pipelines
- **Semantic Versioning Support**: Exit codes align with semantic versioning practices

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

## 📋 Quick Start

### Installation

#### Package Managers (Recommended)

**Windows (Chocolatey):**
```powershell
choco install dotnetapidiff
```

**macOS/Linux (Homebrew):**
```bash
brew install dotnetapidiff
```

#### Quick Install Script (Linux/macOS)

```bash
# Install latest version to user directory
curl -fsSL https://raw.githubusercontent.com/jbrinkman/dotnet-api-diff/main/install.sh | bash

# Install specific version
curl -fsSL https://raw.githubusercontent.com/jbrinkman/dotnet-api-diff/main/install.sh | bash -s -- v1.2.3

# Install system-wide (requires sudo)
curl -fsSL https://raw.githubusercontent.com/jbrinkman/dotnet-api-diff/main/install.sh | sudo bash -s -- --system
```

#### Manual Install

Download the latest release for your platform from [GitHub Releases](https://github.com/jbrinkman/dotnet-api-diff/releases):

- **Linux**: `dotnet-api-diff-linux-x64.tar.gz` or `dotnet-api-diff-linux-arm64.tar.gz`
- **macOS**: `dotnet-api-diff-osx-x64.tar.gz` or `dotnet-api-diff-osx-arm64.tar.gz`
- **Windows**: `dotnet-api-diff-win-x64.zip` or `dotnet-api-diff-win-arm64.zip`

Extract the archive and add the binary to your PATH.

#### Build from Source

```bash
# Clone the repository
git clone https://github.com/jbrinkman/dotnet-api-diff.git
cd dotnet-api-diff

# Build the tool
dotnet build

# Or use the Task runner (recommended)
task build
```

### Basic Usage

```bash
# Compare two assembly versions
dotnet run --project src/DotNetApiDiff -- compare MyLibrary.v1.dll MyLibrary.v2.dll

# Generate a JSON report
dotnet run --project src/DotNetApiDiff -- compare source.dll target.dll --output json

# Save report to file
dotnet run --project src/DotNetApiDiff -- compare source.dll target.dll --output markdown > api-changes.md
```

## 🔧 Usage Examples

### Basic Comparison

```bash
# Simple comparison with console output
dotnet run --project src/DotNetApiDiff -- compare MyLibrary.v1.dll MyLibrary.v2.dll
```

### Filtering Options

```bash
# Filter to specific namespaces
dotnet run --project src/DotNetApiDiff -- compare source.dll target.dll \
  --filter "MyLibrary.Core" --filter "MyLibrary.Extensions"

# Filter to specific type patterns
dotnet run --project src/DotNetApiDiff -- compare source.dll target.dll \
  --type "MyLibrary.Core.*" --type "MyLibrary.Models.*"

# Exclude internal or test types
dotnet run --project src/DotNetApiDiff -- compare source.dll target.dll \
  --exclude "*.Internal*" --exclude "*.Tests*"

# Include internal types (normally excluded)
dotnet run --project src/DotNetApiDiff -- compare source.dll target.dll --include-internals

# Include compiler-generated types
dotnet run --project src/DotNetApiDiff -- compare source.dll target.dll --include-compiler-generated
```

### Output Formats

```bash
# Generate JSON report
dotnet run --project src/DotNetApiDiff -- compare source.dll target.dll --output json

# Generate Markdown report
dotnet run --project src/DotNetApiDiff -- compare source.dll target.dll --output markdown

# Generate XML report
dotnet run --project src/DotNetApiDiff -- compare source.dll target.dll --output xml

# Generate HTML report
dotnet run --project src/DotNetApiDiff -- compare source.dll target.dll --output html
```

### Configuration File Usage

```bash
# Use a configuration file for complex scenarios
dotnet run --project src/DotNetApiDiff -- compare source.dll target.dll --config my-config.json

# Enable verbose logging
dotnet run --project src/DotNetApiDiff -- compare source.dll target.dll --verbose

# Disable colored output (useful for CI/CD)
dotnet run --project src/DotNetApiDiff -- compare source.dll target.dll --no-color
```

## ⚙️ Configuration

### Configuration File

Use JSON configuration files to customize comparison behavior for complex scenarios. This is especially useful for:

- Large libraries with complex namespace structures
- APIs that have been refactored or reorganized
- Automated CI/CD pipelines with specific requirements

### Complete Configuration Example

```json
{
  "mappings": {
    "namespaceMappings": {
      "OldNamespace": ["NewNamespace"],
      "Legacy.Api": ["Modern.Api", "Modern.Api.V2"],
      "MyLibrary.V1": ["MyLibrary.V2.Core", "MyLibrary.V2.Extensions"]
    },
    "typeMappings": {
      "OldNamespace.OldType": "NewNamespace.NewType",
      "Legacy.UserManager": "Modern.Identity.UserService",
      "Utils.StringHelper": "Extensions.StringExtensions"
    },
    "autoMapSameNameTypes": true,
    "ignoreCase": true
  },
  "exclusions": {
    "excludedTypes": [
      "System.Diagnostics.Debug",
      "MyLibrary.Internal.DebugHelper"
    ],
    "excludedTypePatterns": [
      "*.Internal.*",
      "*.Private.*",
      "*.Tests.*",
      "*Helper",
      "*Utility"
    ],
    "excludedMembers": [
      "System.Object.Finalize",
      "MyLibrary.BaseClass.InternalMethod"
    ],
    "excludedMemberPatterns": [
      "*.Obsolete*",
      "*.Debug*",
      "get_Internal*",
      "set_Internal*"
    ]
  },
  "breakingChangeRules": {
    "treatTypeRemovalAsBreaking": true,
    "treatMemberRemovalAsBreaking": true,
    "treatAddedTypeAsBreaking": false,
    "treatAddedMemberAsBreaking": false,
    "treatSignatureChangeAsBreaking": true
  },
  "filters": {
    "includeNamespaces": [
      "MyLibrary.Core",
      "MyLibrary.Extensions",
      "MyLibrary.Models"
    ],
    "excludeNamespaces": [
      "MyLibrary.Internal",
      "MyLibrary.Tests",
      "System.Diagnostics"
    ],
    "includeTypes": [
      "MyLibrary.Core.*",
      "MyLibrary.Models.*"
    ],
    "excludeTypes": [
      "*.Internal*",
      "*.Helper*",
      "*Test*"
    ],
    "includeInternals": false,
    "includeCompilerGenerated": false
  },
  "outputFormat": "Console",
  "outputPath": null,
  "failOnBreakingChanges": true
}
```

### Sample Configuration Files

The repository includes several sample configuration files for common scenarios:

- **`samples/basic-config.json`** - Basic configuration for simple libraries
- **`samples/enterprise-config.json`** - Configuration for large enterprise libraries
- **`samples/strict-breaking-changes.json`** - Strict breaking change detection
- **`samples/lenient-changes.json`** - Lenient configuration for pre-release versions
- **`samples/namespace-filtering.json`** - Advanced namespace filtering examples

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

## 🔄 CI/CD Integration

### GitHub Actions

Create `.github/workflows/api-compatibility.yml`:

```yaml
name: API Compatibility Check

on:
  pull_request:
    branches: [ main ]

jobs:
  api-compatibility:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v4
      with:
        fetch-depth: 0  # Need full history to compare with main
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: 8.0.x
    
    - name: Build current version
      run: dotnet build --configuration Release
    
    - name: Checkout main branch
      run: |
        git checkout main
        dotnet build --configuration Release --output ./baseline
        git checkout -
    
    - name: Build PR version
      run: dotnet build --configuration Release --output ./current
    
    - name: Run API Diff
      run: |
        dotnet run --project path/to/dotnet-api-diff -- compare \
          ./baseline/MyLibrary.dll \
          ./current/MyLibrary.dll \
          --config .github/api-diff-config.json \
          --output markdown \
          --no-color > api-changes.md
    
    - name: Comment PR with API Changes
      uses: actions/github-script@v7
      with:
        script: |
          const fs = require('fs');
          const apiChanges = fs.readFileSync('api-changes.md', 'utf8');
          github.rest.issues.createComment({
            issue_number: context.issue.number,
            owner: context.repo.owner,
            repo: context.repo.repo,
            body: `## API Changes\n\n${apiChanges}`
          });
```

### Azure DevOps Pipeline

Create `azure-pipelines-api-check.yml`:

```yaml
trigger:
- main

pool:
  vmImage: 'ubuntu-latest'

variables:
  buildConfiguration: 'Release'

steps:
- task: DotNetCoreCLI@2
  displayName: 'Build Baseline'
  inputs:
    command: 'build'
    configuration: $(buildConfiguration)
    outputDir: '$(Build.ArtifactStagingDirectory)/baseline'

- task: DotNetCoreCLI@2
  displayName: 'Build Current'
  inputs:
    command: 'build'
    configuration: $(buildConfiguration)
    outputDir: '$(Build.ArtifactStagingDirectory)/current'

- task: DotNetCoreCLI@2
  displayName: 'Run API Diff'
  inputs:
    command: 'run'
    projects: 'tools/dotnet-api-diff/DotNetApiDiff.csproj'
    arguments: >
      compare
      $(Build.ArtifactStagingDirectory)/baseline/MyLibrary.dll
      $(Build.ArtifactStagingDirectory)/current/MyLibrary.dll
      --config api-diff-config.json
      --output json
      --no-color
  continueOnError: true

- task: PublishTestResults@2
  displayName: 'Publish API Diff Results'
  inputs:
    testResultsFormat: 'JUnit'
    testResultsFiles: 'api-diff-results.xml'
```

## 📊 Exit Codes

The tool uses semantic exit codes to integrate with CI/CD systems:

- **0**: Success - No breaking changes detected
- **1**: Warning - Non-breaking changes detected (additions only)
- **2**: Error - Breaking changes detected
- **3**: Critical - Assembly loading or parsing errors
- **4**: Configuration - Invalid configuration or command-line arguments
- **99**: Unexpected - Unhandled exceptions or system errors

### Using Exit Codes in Scripts

```bash
#!/bin/bash

# Run API diff and capture exit code
dotnet run --project dotnet-api-diff -- compare old.dll new.dll --config config.json
EXIT_CODE=$?

case $EXIT_CODE in
  0)
    echo "✅ No breaking changes detected"
    ;;
  1)
    echo "⚠️  Non-breaking changes detected"
    ;;
  2)
    echo "❌ Breaking changes detected - blocking release"
    exit 1
    ;;
  3)
    echo "💥 Assembly loading failed"
    exit 1
    ;;
  4)
    echo "⚙️  Configuration error"
    exit 1
    ;;
  *)
    echo "🔥 Unexpected error"
    exit 1
    ;;
esac
```

## 🎯 Real-World Examples

### Example 1: NuGet Package Release Check

```bash
# Download previous version from NuGet
nuget install MyLibrary -Version 1.0.0 -OutputDirectory packages

# Compare with current build
dotnet run --project dotnet-api-diff -- compare \
  packages/MyLibrary.1.0.0/lib/net8.0/MyLibrary.dll \
  src/MyLibrary/bin/Release/net8.0/MyLibrary.dll \
  --output json > api-changes.json

# Parse results for automated decision making
if [ $? -eq 2 ]; then
  echo "Breaking changes detected - increment major version"
elif [ $? -eq 1 ]; then
  echo "Non-breaking changes detected - increment minor version"
else
  echo "No API changes - increment patch version"
fi
```

### Example 2: Multi-Target Framework Analysis

```bash
# Compare .NET Framework vs .NET Core implementations
dotnet run --project dotnet-api-diff -- compare \
  MyLibrary.net48.dll \
  MyLibrary.net8.0.dll \
  --config framework-migration-config.json \
  --output html > framework-differences.html
```

### Example 3: Pre-Release Validation

```bash
# Lenient checking for pre-release versions
dotnet run --project dotnet-api-diff -- compare \
  MyLibrary.1.0.0-stable.dll \
  MyLibrary.1.1.0-beta.dll \
  --config samples/lenient-changes.json \
  --output markdown
```

## 📖 Command Line Reference

### Compare Command

- `<sourceAssembly>`: Path to the source/baseline assembly (required)
- `<targetAssembly>`: Path to the target/current assembly (required)
- `--config, -c`: Path to configuration file
- `--output, -o`: Output format (console, json, xml, html, markdown)
- `--filter, -f`: Filter to specific namespaces (can be specified multiple times)
- `--type, -t`: Filter to specific type patterns (can be specified multiple times)
- `--exclude, -e`: Exclude types matching pattern (can be specified multiple times)
- `--include-internals`: Include internal types in the comparison
- `--include-compiler-generated`: Include compiler-generated types in the comparison
- `--no-color`: Disable colored output (useful for CI/CD)
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

## 🔍 Troubleshooting

### Common Issues

**Assembly Loading Errors**

```bash
# Ensure assemblies are built for compatible target frameworks
dotnet run --project dotnet-api-diff -- compare source.dll target.dll --verbose
```

**Missing Dependencies**

```bash
# Place all dependencies in the same directory as the assemblies
# Or use the --assembly-path option to specify additional search paths
```

**Large Memory Usage**

```bash
# For very large assemblies, consider filtering to specific namespaces
dotnet run --project dotnet-api-diff -- compare source.dll target.dll \
  --filter "MyLibrary.Core" --exclude "*.Internal*"
```

**Performance Issues**

```bash
# Use configuration files to exclude unnecessary types
dotnet run --project dotnet-api-diff -- compare source.dll target.dll \
  --config samples/enterprise-config.json
```

### Debug Mode

Enable detailed logging for troubleshooting:

```bash
# Set environment variable for detailed logging
export DOTNET_API_DIFF_LOG_LEVEL=Debug

# Run with verbose output
dotnet run --project dotnet-api-diff -- compare source.dll target.dll --verbose
```

## 🚀 Advanced Usage

### Custom Report Formats

Extend the tool with custom report generators:

```csharp
public class CustomReportGenerator : IReportGenerator
{
    public string GenerateReport(ComparisonResult result, ReportFormat format)
    {
        // Custom report logic
        return customReport;
    }
}
```

### Programmatic Usage

Use the tool as a library in your own applications:

```csharp
var services = new ServiceCollection();
// Configure services...
var serviceProvider = services.BuildServiceProvider();

var comparer = serviceProvider.GetRequiredService<IApiComparer>();
var result = await comparer.CompareAssembliesAsync(sourceAssembly, targetAssembly);

if (result.HasBreakingChanges)
{
    Console.WriteLine($"Found {result.BreakingChangesCount} breaking changes");
}
```

### Integration with Build Tools

**MSBuild Integration**

```xml
<Target Name="CheckApiCompatibility" BeforeTargets="Pack">
  <Exec Command="dotnet run --project $(MSBuildThisFileDirectory)tools/dotnet-api-diff -- compare $(PreviousVersionDll) $(OutputPath)$(AssemblyName).dll --config api-config.json" 
        ContinueOnError="false" />
</Target>
```

**Cake Build Integration**

```csharp
Task("CheckApiCompatibility")
    .Does(() =>
{
    var exitCode = StartProcess("dotnet", new ProcessSettings
    {
        Arguments = "run --project tools/dotnet-api-diff -- compare baseline.dll current.dll --config config.json"
    });
    
    if (exitCode == 2)
    {
        throw new Exception("Breaking changes detected!");
    }
});
```

## 🤝 Contributing

We welcome contributions! Here's how to get started:

### Development Setup

```bash
# Clone the repository
git clone https://github.com/jbrinkman/dotnet-api-diff.git
cd dotnet-api-diff

# Install dependencies
dotnet restore

# Run tests
task test

# Run with coverage
task coverage
```

### Contribution Guidelines

1. **Fork the repository** and create a feature branch
2. **Write tests** for new functionality
3. **Follow coding standards** (enforced by StyleCop)
4. **Update documentation** for user-facing changes
5. **Ensure all tests pass** including code coverage requirements
6. **Submit a pull request** with a clear description

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with [Spectre.Console](https://spectreconsole.net/) for beautiful console output
- Uses [System.Reflection.Metadata](https://www.nuget.org/packages/System.Reflection.Metadata/) for efficient assembly analysis
- Inspired by the need for better API compatibility tooling in the .NET ecosystem

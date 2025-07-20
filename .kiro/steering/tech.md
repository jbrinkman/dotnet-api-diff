# Technical Stack

## Framework and Language

- .NET 8.0
- C# with nullable reference types enabled
- Console application

## Dependencies

- Microsoft.Extensions.DependencyInjection - For dependency injection
- Microsoft.Extensions.Logging - For structured logging
- System.Reflection.Metadata - For assembly inspection

## Testing Framework

- xUnit - Primary testing framework
- Moq - Mocking library
- coverlet - Code coverage tool

## Build System

The project uses multiple build systems:

1. **MSBuild/dotnet CLI** - Primary build system
2. **Taskfile** - Task runner for common development workflows

## Common Commands

### Basic dotnet commands

```bash
# Build the solution
dotnet build

# Run tests
dotnet test

# Run the application
dotnet run --project src/DotNetApiDiff/DotNetApiDiff.csproj -- --old old.dll --new new.dll
```

### Taskfile commands

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

# Format code
task code:format

# Run code quality checks
task code:quality

# Run CI build sequence
task ci
```

## Code Style and Analysis

- StyleCop.Analyzers - For code style enforcement
- Microsoft.CodeAnalysis.NetAnalyzers - For .NET code quality analysis
- .editorconfig - For consistent code style across editors

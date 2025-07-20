# Project Structure

## Solution Organization

The solution follows a clean separation between source code and tests:

```
DotNetApiDiff.sln
├── src/
│   └── DotNetApiDiff/           # Main project
├── tests/
│   └── DotNetApiDiff.Tests/     # Test project
└── scripts/                     # Utility scripts
```

## Source Code Organization

The main project is organized by feature/responsibility:

```
src/DotNetApiDiff/
├── ApiExtraction/               # API extraction and comparison logic
│   ├── ApiComparer.cs           # Compares APIs between assemblies
│   ├── ApiExtractor.cs          # Extracts API information from assemblies
│   ├── DifferenceCalculator.cs  # Calculates differences between APIs
│   ├── MemberSignatureBuilder.cs # Builds member signatures
│   ├── NameMapper.cs            # Maps names between assemblies
│   └── TypeAnalyzer.cs          # Analyzes type information
├── AssemblyLoading/             # Assembly loading functionality
│   ├── AssemblyLoader.cs        # Loads assemblies for analysis
│   └── IsolatedAssemblyLoadContext.cs # Isolates assembly loading
├── Interfaces/                  # Core service interfaces
│   ├── IApiComparer.cs
│   ├── IApiExtractor.cs
│   ├── IAssemblyLoader.cs
│   ├── IDifferenceCalculator.cs
│   ├── IMemberSignatureBuilder.cs
│   ├── INameMapper.cs
│   ├── IReportGenerator.cs
│   └── ITypeAnalyzer.cs
├── Models/                      # Data models
│   ├── ApiChange.cs
│   ├── ApiComparison.cs
│   ├── ApiDifference.cs
│   ├── ApiMember.cs
│   ├── ComparisonResult.cs
│   ├── Enums.cs
│   └── Configuration/           # Configuration models
│       ├── BreakingChangeRules.cs
│       ├── ComparisonConfiguration.cs
│       ├── ExclusionConfiguration.cs
│       ├── FilterConfiguration.cs
│       └── MappingConfiguration.cs
└── Program.cs                   # Main entry point
```

## Test Organization

Tests mirror the structure of the main project:

```
tests/DotNetApiDiff.Tests/
├── ApiExtraction/               # Tests for API extraction components
├── Assembly/                    # Tests for assembly loading
├── Models/                      # Tests for data models
│   └── Configuration/           # Tests for configuration models
└── coverage.info                # Coverage report
```

## Architecture Patterns

1. **Dependency Injection** - The application uses Microsoft's DI container for service resolution
2. **Interface-based Design** - All components are defined by interfaces in the Interfaces folder
3. **Clean Separation of Concerns** - Each component has a single responsibility
4. **Configuration Objects** - Dedicated configuration classes in Models/Configuration

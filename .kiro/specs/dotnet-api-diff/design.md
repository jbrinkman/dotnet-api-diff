# Design Document

## Overview

The .NET API Diff tool is a command-line application built on .NET 8 that compares the public API surface between two .NET assemblies. The tool uses .NET's reflection capabilities to analyze assembly metadata, extract public members, and generate detailed comparison reports. It supports namespace/type mapping for compatibility analysis and provides multiple output formats suitable for both human consumption and automated processing.

## Architecture

The application follows a layered architecture with clear separation of concerns:

```
┌─────────────────────────────────────────┐
│              CLI Layer                  │
│  (Argument parsing, output formatting)  │
└─────────────────────────────────────────┘
                    │
┌─────────────────────────────────────────┐
│           Application Layer             │
│     (Orchestration, configuration)      │
└─────────────────────────────────────────┘
                    │
┌─────────────────────────────────────────┐
│            Domain Layer                 │
│  (Comparison logic, mapping, analysis)  │
└─────────────────────────────────────────┘
                    │
┌─────────────────────────────────────────┐
│         Infrastructure Layer            │
│   (Assembly loading, file I/O, JSON)    │
└─────────────────────────────────────────┘
```

## Components and Interfaces

### 1. CLI Component

- **CommandLineParser**: Handles argument parsing using System.CommandLine
- **OutputFormatter**: Formats results for console, JSON, and markdown output
- **ExitCodeManager**: Manages application exit codes based on comparison results

### 2. Configuration Component

- **ConfigurationLoader**: Loads and validates configuration from files or command line
- **MappingConfiguration**: Defines namespace and type name mappings
- **ExclusionConfiguration**: Defines intentionally excluded types and members
- **BreakingChangeRules**: Defines what constitutes breaking vs non-breaking changes

### 3. Assembly Analysis Component

- **AssemblyLoader**: Safely loads .NET assemblies using Assembly.LoadFrom with proper error handling
- **ApiExtractor**: Extracts public API members from loaded assemblies using reflection
- **TypeAnalyzer**: Analyzes individual types and their public members
- **MemberSignatureBuilder**: Creates normalized signatures for methods, properties, events

### 4. Comparison Engine

- **ApiComparer**: Core comparison logic that matches and compares API elements
- **NameMapper**: Applies namespace and type name mappings during comparison
- **ChangeClassifier**: Classifies changes as breaking, non-breaking, or excluded
- **DifferenceCalculator**: Identifies additions, removals, and modifications

### 5. Reporting Component

- **ComparisonReport**: Data model representing the complete comparison results
- **ReportGenerator**: Generates reports in different formats
- **ChangeFormatter**: Formats individual changes with appropriate detail level

## Data Models

### Core Models

```csharp
public class ApiMember
{
    public string Name { get; set; }
    public string FullName { get; set; }
    public MemberType Type { get; set; } // Class, Method, Property, Event, Field
    public AccessibilityLevel Accessibility { get; set; }
    public string Signature { get; set; }
    public List<string> Attributes { get; set; }
    public string DeclaringType { get; set; }
    public string Namespace { get; set; }
}

public class ApiComparison
{
    public List<ApiChange> Additions { get; set; }
    public List<ApiChange> Removals { get; set; }
    public List<ApiChange> Modifications { get; set; }
    public List<ApiChange> Excluded { get; set; }
    public ComparisonSummary Summary { get; set; }
}

public class ApiChange
{
    public ChangeType Type { get; set; } // Added, Removed, Modified, Excluded
    public ApiMember SourceMember { get; set; }
    public ApiMember TargetMember { get; set; }
    public bool IsBreakingChange { get; set; }
    public string Description { get; set; }
    public List<string> Details { get; set; }
}
```

### Configuration Models

```csharp
public class ComparisonConfiguration
{
    public Dictionary<string, List<string>> NamespaceMappings { get; set; }
    public Dictionary<string, string> TypeMappings { get; set; }
    public List<string> ExcludedTypes { get; set; }
    public List<string> ExcludedMembers { get; set; }
    public BreakingChangeRules BreakingChangeRules { get; set; }
    public FilterConfiguration Filters { get; set; }
}
```

## Error Handling

### Assembly Loading Errors

- **FileNotFoundException**: Clear message indicating which assembly file was not found
- **BadImageFormatException**: Specific error for invalid or corrupted assembly files
- **ReflectionTypeLoadException**: Detailed reporting of types that failed to load
- **SecurityException**: Handling for assemblies that cannot be loaded due to security restrictions

### Configuration Errors

- **Invalid JSON/YAML**: Detailed parsing error messages with line numbers
- **Missing Mappings**: Validation that all referenced mappings exist
- **Circular References**: Detection and reporting of circular namespace mappings
- **Pattern Validation**: Validation of wildcard patterns and regex expressions

### Runtime Errors

- **OutOfMemoryException**: Graceful handling for very large assemblies
- **StackOverflowException**: Protection against deeply nested type hierarchies
- **Timeout Handling**: Configurable timeouts for long-running comparisons

## Testing Strategy

### Unit Testing

- **Assembly Loading**: Mock assemblies with known public APIs for consistent testing
- **Comparison Logic**: Test all change detection scenarios with controlled inputs
- **Configuration Parsing**: Validate all configuration options and error conditions
- **Output Formatting**: Verify correct formatting for all supported output types
- **Mapping Logic**: Test namespace and type name mapping with various scenarios

### Integration Testing

- **Real Assembly Comparison**: Test with actual .NET assemblies (System.Collections, etc.)
- **Configuration File Loading**: Test with sample configuration files
- **CLI Argument Parsing**: Test all command-line argument combinations
- **Exit Code Validation**: Verify correct exit codes for different scenarios

### Performance Testing

- **Large Assembly Handling**: Test with assemblies containing thousands of types
- **Memory Usage**: Monitor memory consumption during comparison
- **Execution Time**: Benchmark comparison speed for different assembly sizes

### Compatibility Testing

- **Framework Versions**: Test loading assemblies targeting different .NET versions
- **Platform Differences**: Verify behavior across Windows, Linux, and macOS
- **Assembly Formats**: Test with different assembly types (.dll, .exe, reference assemblies)

## Implementation Considerations

### Reflection Strategy

- Use `Assembly.LoadFrom()` with custom `AssemblyLoadContext` for isolation
- Implement proper disposal patterns for loaded assemblies
- Handle missing dependencies gracefully with `AssemblyResolve` events
- Cache reflection results to improve performance for repeated operations

### Signature Comparison

- Normalize method signatures to handle generic type parameters consistently
- Account for default parameter values and optional parameters
- Handle ref/out parameter differences appropriately
- Consider covariance and contravariance in generic types

### Configuration Management

- Support both JSON and YAML configuration formats
- Allow configuration inheritance and composition
- Provide sensible defaults for common scenarios
- Validate configuration at startup with clear error messages

### Output Generation

- Use streaming for large reports to manage memory usage
- Implement proper escaping for JSON and markdown output
- Support colored console output with fallback for non-terminal environments
- Provide progress indicators for long-running comparisons

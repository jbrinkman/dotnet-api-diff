# Requirements Document

## Introduction

This feature involves creating a .NET 8 CLI application that analyzes and compares the public API surface between two .NET assemblies. The tool will identify differences in public members (classes, methods, properties, events, etc.) and provide detailed reporting suitable for use in CI/CD pipelines, particularly GitHub workflows. This enables teams to track API changes, ensure backward compatibility, and maintain API versioning standards.

## Requirements

### Requirement 1

**User Story:** As a developer, I want to compare two .NET assemblies to identify public API differences, so that I can understand what has changed between versions.

#### Acceptance Criteria

1. WHEN the CLI tool is executed with two assembly file paths THEN the system SHALL load both assemblies and extract their public API members
2. WHEN both assemblies are successfully loaded THEN the system SHALL compare all public types, methods, properties, events, and fields
3. WHEN the comparison is complete THEN the system SHALL generate a detailed report of differences including additions, removals, and modifications
4. IF an assembly file cannot be loaded THEN the system SHALL display a clear error message and exit with a non-zero code

### Requirement 2

**User Story:** As a CI/CD engineer, I want the tool to run in GitHub workflows with appropriate exit codes, so that I can fail builds when breaking changes are detected.

#### Acceptance Criteria

1. WHEN the tool detects breaking changes THEN the system SHALL exit with a non-zero exit code
2. WHEN only non-breaking changes are detected THEN the system SHALL exit with exit code 0
3. WHEN the tool is run with a help flag THEN the system SHALL display usage instructions and available options
4. WHEN the tool encounters an error THEN the system SHALL log the error to stderr and exit with an appropriate error code

### Requirement 3

**User Story:** As a developer, I want to configure what constitutes a breaking change, so that I can customize the tool for my project's compatibility requirements.

#### Acceptance Criteria

1. WHEN the tool is run with configuration options THEN the system SHALL respect settings for what changes are considered breaking
2. WHEN a configuration file is provided THEN the system SHALL load and apply the specified rules
3. IF no configuration is provided THEN the system SHALL use sensible defaults for breaking change detection
4. WHEN invalid configuration is provided THEN the system SHALL display validation errors and exit with a non-zero code

### Requirement 4

**User Story:** As a developer, I want detailed output formats including JSON and markdown, so that I can integrate the results with different tools and documentation systems.

#### Acceptance Criteria

1. WHEN the tool is run with a format option THEN the system SHALL output results in the specified format (console, JSON, markdown)
2. WHEN JSON format is selected THEN the system SHALL produce valid, structured JSON output suitable for programmatic consumption
3. WHEN markdown format is selected THEN the system SHALL generate human-readable markdown suitable for documentation
4. WHEN console format is used THEN the system SHALL display a clear, colored summary of changes

### Requirement 5

**User Story:** As a developer, I want to filter the comparison to specific namespaces or types, so that I can focus on relevant parts of large assemblies.

#### Acceptance Criteria

1. WHEN namespace filters are provided THEN the system SHALL only compare types within the specified namespaces
2. WHEN type name patterns are provided THEN the system SHALL only compare types matching the specified patterns
3. WHEN exclusion filters are provided THEN the system SHALL ignore types or namespaces matching the exclusion criteria
4. IF no types match the provided filters THEN the system SHALL display a warning and continue with an empty comparison

### Requirement 6

**User Story:** As a developer, I want to map namespaces and type names between assemblies, so that I can compare APIs that have been renamed for trademark or branding reasons while maintaining compatibility.

#### Acceptance Criteria

1. WHEN namespace mapping configuration is provided THEN the system SHALL map source namespaces to target namespaces during comparison
2. WHEN type name mapping configuration is provided THEN the system SHALL map source type names to target type names during comparison
3. WHEN a source namespace maps to multiple target namespaces THEN the system SHALL check all mapped target namespaces for equivalent types
4. WHEN mapped types are found THEN the system SHALL compare their public members as if they were the same type
5. IF a mapped type is not found in the target assembly THEN the system SHALL report it as a missing type
6. WHEN mapping configuration is invalid THEN the system SHALL display clear validation errors

### Requirement 7

**User Story:** As a developer, I want to specify types and members that are intentionally excluded from the target API, so that I can document known unsupported features rather than treating them as missing.

#### Acceptance Criteria

1. WHEN an exclusion list is provided THEN the system SHALL treat specified types and members as intentionally excluded rather than missing
2. WHEN excluded types or members are encountered THEN the system SHALL report them in a separate "Excluded/Unsupported" section of the output
3. WHEN excluded items are found THEN the system SHALL NOT treat them as breaking changes or errors
4. WHEN exclusion patterns are provided THEN the system SHALL support wildcard matching for types and member names
5. IF an excluded item is actually present in the target assembly THEN the system SHALL report it as an unexpected inclusion

### Requirement 8

**User Story:** As a developer, I want to see detailed information about each API change, so that I can understand the impact and make informed decisions.

#### Acceptance Criteria

1. WHEN a type is added THEN the system SHALL report the full type signature and accessibility level
2. WHEN a type is removed THEN the system SHALL report the removed type and mark it as a potential breaking change
3. WHEN a method signature changes THEN the system SHALL show both the old and new signatures with highlighted differences
4. WHEN member accessibility changes THEN the system SHALL report the visibility change and its breaking change status
5. WHEN comparing mapped types THEN the system SHALL show both the original and mapped names in the output for clarity

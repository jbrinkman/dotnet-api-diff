# Implementation Plan

## Git Workflow

Each task should follow this git workflow:

1. Create a new feature branch: `git checkout -b feature/task-X-description`
2. Complete the task implementation and tests
3. Commit changes with descriptive message
4. Push branch to GitHub: `git push origin feature/task-X-description`
5. Create Pull Request for code review
6. After PR approval and merge, checkout main and pull latest changes
7. Delete the feature branch locally and remotely

- [x] 1. Set up project structure and core interfaces
  - Create GitHub repository and clone locally
  - Initialize git repository with proper .gitignore for .NET projects
  - Create .NET 8 console application with proper project structure (src/, tests/, docs/ folders)
  - Set up solution file and project references
  - Define core interfaces for assembly loading, comparison, and reporting
  - Set up dependency injection container and logging framework
  - Create initial README.md with project description and build instructions
  - **Git Workflow**: Create branch `feature/task-1-project-setup`, commit, push, and create PR
  - _Requirements: 1.1, 2.3_

- [x] 2. Implement data models and validation
  - [x] 2.1 Create core data model classes
    - Implement ApiMember, ApiComparison, ApiChange classes with proper validation
    - Create enums for MemberType, ChangeType, AccessibilityLevel
    - Write unit tests for data model validation and serialization
    - **Git Workflow**: Create branch `feature/task-2.1-data-models`, commit, push, and create PR
    - _Requirements: 7.1, 8.1_

  - [x] 2.2 Implement configuration models
    - Create ComparisonConfiguration, MappingConfiguration, ExclusionConfiguration classes
    - Implement JSON/YAML deserialization with validation
    - Write unit tests for configuration loading and validation
    - **Git Workflow**: Create branch `feature/task-2.2-config-models`, commit, push, and create PR
    - _Requirements: 3.1, 3.2, 6.1, 7.1_

- [ ] 3. Create assembly loading and analysis components
  - [x] 3.1 Implement AssemblyLoader with error handling
    - Write AssemblyLoader class using Assembly.LoadFrom with proper exception handling
    - Implement AssemblyLoadContext for assembly isolation
    - Create unit tests with mock assemblies and error scenarios
    - **Git Workflow**: Create branch `feature/task-3.1-assembly-loader`, commit, push, and create PR
    - _Requirements: 1.1, 1.4_

  - [x] 3.2 Implement ApiExtractor for reflection-based analysis
    - Create ApiExtractor to extract public types, methods, properties, events, fields
    - Implement TypeAnalyzer for detailed type member analysis
    - Write MemberSignatureBuilder for normalized method signatures
    - Create unit tests using System.Collections and other known assemblies
    - **Git Workflow**: Create branch `feature/task-3.2-api-extractor`, commit, push, and create PR
    - _Requirements: 1.1, 1.2, 8.1_

  - [x] 3.3 Implement GitHub workflows for CI/CD
    - Create workflow for building and testing on pull requests
    - Create workflow for building and testing on main branch pushes
    - Configure workflows to treat warnings as errors
    - Add status badges to README.md
    - **Git Workflow**: Create branch `feature/task-3.3-github-workflows`, commit, push, and create PR
    - _Requirements: 2.1, 2.2, 2.3, 2.4_

  - [x] 3.4 Implement Taskfile.dev for common project tasks
    - Create Taskfile.yml with common dotnet tasks (build, test, coverage)
    - Add tasks for generating and viewing coverage reports
    - Include tasks for running specific test categories
    - Document Taskfile usage in README.md
    - **Git Workflow**: Create branch `feature/task-3.4-taskfile`, commit, push, and create PR
    - _Requirements: 2.3, 4.4_

- [ ] 4. Build comparison engine core functionality
  - [x] 4.1 Implement basic API comparison logic
    - Create ApiComparer class to identify additions, removals, and modifications
    - Implement DifferenceCalculator for detailed change analysis
    - Write unit tests comparing known assembly versions
    - **Git Workflow**: Create branch `feature/task-4.1-api-comparer`, commit, push, and create PR
    - _Requirements: 1.2, 1.3, 8.2, 8.3_

  - [x] 4.2 Add namespace and type mapping capabilities
    - Implement NameMapper class for namespace and type name transformations
    - Integrate mapping logic into ApiComparer workflow
    - Create unit tests for various mapping scenarios including one-to-many mappings
    - **Git Workflow**: Create branch `feature/task-4.2-name-mapping`, commit, push, and create PR
    - _Requirements: 6.1, 6.2, 6.3, 6.4_

  - [x] 4.3 Implement exclusion and breaking change classification
    - Create ChangeClassifier to categorize changes as breaking, non-breaking, or excluded
    - Implement exclusion pattern matching with wildcard support
    - Write unit tests for breaking change detection and exclusion logic
    - **Git Workflow**: Create branch `feature/task-4.3-change-classification`, commit, push, and create PR
    - _Requirements: 3.1, 7.1, 7.2, 7.3, 7.4_

- [x] 5. Create CLI interface and argument parsing
  - [x] 5.1 Implement command-line interface using Spectre.Console.Cli
    - Create CLI commands and options for assembly paths, configuration, output format
    - Implement help text and usage examples
    - Write integration tests for CLI argument parsing
    - **Git Workflow**: Create branch `feature/task-5.1-cli-interface`, commit, push, and create PR
    - _Requirements: 2.3, 4.1_

  - [x] 5.2 Add filtering and configuration options
    - Implement namespace and type filtering command-line options
    - Add configuration file loading from CLI arguments
    - Create integration tests for filtering and configuration scenarios
    - **Git Workflow**: Create branch `feature/task-5.2-cli-filtering`, commit, push, and create PR
    - _Requirements: 3.2, 3.3, 5.1, 5.2, 5.3, 5.4_

- [ ] 6. Implement output formatting and reporting
  - [x] 6.1 Create ReportGenerator and console output formatter
    - Implement ReportGenerator interface with support for multiple formats
    - Create ConsoleFormatter with colored output for additions, removals, modifications
    - Add summary statistics and breaking change indicators
    - Write unit tests for console output formatting
    - **Git Workflow**: Create branch `feature/task-6.1-report-generator`, commit, push, and create PR
    - _Requirements: 4.4, 8.1, 8.4_

  - [x] 6.2 Implement JSON output formatter
    - Create JsonFormatter producing structured, valid JSON output
    - Include all comparison details suitable for programmatic consumption
    - Write unit tests validating JSON structure and content
    - **Git Workflow**: Create branch `feature/task-6.2-json-formatter`, commit, push, and create PR
    - _Requirements: 4.1, 4.2_

  - [x] 6.3 Implement markdown output formatter
    - Create MarkdownFormatter generating human-readable documentation
    - Include tables, sections for different change types, and exclusions
    - Write unit tests for markdown formatting and structure
    - **Git Workflow**: Create branch `feature/task-6.3-markdown-formatter`, commit, push, and create PR
    - _Requirements: 4.1, 4.3, 7.2_

- [ ] 7. Add exit code management and error handling
  - [x] 7.1 Implement exit code logic based on comparison results
    - Create ExitCodeManager to determine appropriate exit codes
    - Implement logic for breaking changes, errors, and successful comparisons
    - Write unit tests for all exit code scenarios
    - **Git Workflow**: Create branch `feature/task-7.1-exit-codes`, commit, push, and create PR
    - _Requirements: 2.1, 2.2, 2.4_

  - [ ] 7.2 Add comprehensive error handling and logging
    - Implement structured logging throughout the application
    - Add proper exception handling for assembly loading, configuration, and comparison errors
    - Write integration tests for error scenarios and recovery
    - **Git Workflow**: Create branch `feature/task-7.2-error-handling`, commit, push, and create PR
    - _Requirements: 1.4, 3.4, 6.6_

- [ ] 8. Create integration tests and end-to-end scenarios
  - [ ] 8.1 Build test assembly pairs for integration testing
    - Create sample assemblies with known API differences for testing
    - Include scenarios with namespace mappings, exclusions, and breaking changes
    - Write integration tests using these test assemblies
    - **Git Workflow**: Create branch `feature/task-8.1-integration-tests`, commit, push, and create PR
    - _Requirements: 1.1, 1.2, 1.3_

  - [ ] 8.2 Test complete workflows with configuration files
    - Create sample configuration files for different use cases
    - Test end-to-end workflows from CLI input to formatted output
    - Validate exit codes and error handling in realistic scenarios
    - **Git Workflow**: Create branch `feature/task-8.2-workflow-tests`, commit, push, and create PR
    - _Requirements: 3.1, 3.2, 6.1, 6.2, 7.1_

- [ ] 9. Add performance optimizations and final polish
  - [ ] 9.1 Optimize memory usage and performance for large assemblies
    - Implement streaming and lazy loading where appropriate
    - Add progress reporting for long-running comparisons
    - Write performance tests with large assemblies
    - **Git Workflow**: Create branch `feature/task-9.1-performance`, commit, push, and create PR
    - _Requirements: 1.1, 1.2_

  - [ ] 9.2 Final integration and documentation
    - Create comprehensive README with usage examples
    - Add sample configuration files and GitHub workflow examples
    - Perform final end-to-end testing with real-world assemblies
    - **Git Workflow**: Create branch `feature/task-9.2-documentation`, commit, push, and create PR
    - _Requirements: 2.3, 4.1, 4.2, 4.3_

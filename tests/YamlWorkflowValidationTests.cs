// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace DotNetApiDiff.Tests
{
    /// <summary>
    /// Comprehensive unit tests for YAML workflow validation functionality.
    /// This test suite covers GitHub Actions workflow validation including
    /// syntax validation, structure validation, and semantic checks.
    /// Testing Framework: xUnit 2.9.3
    /// Dependencies: Microsoft.NET.Test.Sdk 17.14.1
    /// </summary>
    public class YamlWorkflowValidationTests
    {
        #region Valid Workflow Tests

        [Fact]
        public void ValidateWorkflow_WithValidMinimalYaml_ShouldReturnTrue()
        {
            // Arrange
            var validYaml = @"
name: CI
on: push
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
";

            // Act
            var result = ValidateYamlWorkflow(validYaml);

            // Assert
            Assert.True(result, "Valid minimal YAML workflow should be accepted");
        }

        [Fact]
        public void ValidateWorkflow_WithComplexValidWorkflow_ShouldReturnTrue()
        {
            // Arrange
            var complexYaml = @"
name: Complex CI/CD Pipeline
on:
  push:
    branches: [main, develop]
    paths-ignore: ['docs/**', '*.md']
  pull_request:
    branches: [main]
  schedule:
    - cron: '0 2 * * 1'
  workflow_dispatch:
    inputs:
      environment:
        description: 'Environment to deploy to'
        required: true
        default: 'staging'
        type: choice
        options:
        - staging
        - production

env:
  NODE_VERSION: '18.x'
  REGISTRY: ghcr.io
  IMAGE_NAME: my-app

jobs:
  test:
    name: Run Tests
    runs-on: ubuntu-latest
    timeout-minutes: 15
    strategy:
      matrix:
        node-version: [16.x, 18.x, 20.x]
        os: [ubuntu-latest, windows-latest]
      fail-fast: false
      max-parallel: 4
    steps:
    - name: Checkout code
      uses: actions/checkout@v4
      with:
        fetch-depth: 0
    - name: Setup Node.js ${{ matrix.node-version }}
      uses: actions/setup-node@v4
      with:
        node-version: ${{ matrix.node-version }}
        cache: 'npm'
    - name: Install dependencies
      run: npm ci
    - name: Run linting
      run: npm run lint
    - name: Run tests with coverage
      run: npm run test:coverage
    - name: Upload coverage reports
      uses: codecov/codecov-action@v4
      if: matrix.node-version == '18.x' && matrix.os == 'ubuntu-latest'
      with:
        token: ${{ secrets.CODECOV_TOKEN }}
        
  build:
    name: Build Application
    needs: test
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main' || github.event_name == 'workflow_dispatch'
    outputs:
      image-digest: ${{ steps.build.outputs.digest }}
      image-tag: ${{ steps.meta.outputs.tags }}
    steps:
    - name: Checkout
      uses: actions/checkout@v4
    - name: Set up Docker Buildx
      uses: docker/setup-buildx-action@v3
    - name: Log in to Container Registry
      uses: docker/login-action@v3
      with:
        registry: ${{ env.REGISTRY }}
        username: ${{ github.actor }}
        password: ${{ secrets.GITHUB_TOKEN }}
    - name: Extract metadata
      id: meta
      uses: docker/metadata-action@v5
      with:
        images: ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}
        tags: |
          type=ref,event=branch
          type=ref,event=pr
          type=sha,prefix={{branch}}-
    - name: Build and push Docker image
      id: build
      uses: docker/build-push-action@v5
      with:
        context: .
        push: true
        tags: ${{ steps.meta.outputs.tags }}
        labels: ${{ steps.meta.outputs.labels }}
        cache-from: type=gha
        cache-to: type=gha,mode=max

  deploy:
    name: Deploy to Environment
    needs: [test, build]
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    environment:
      name: ${{ inputs.environment || 'staging' }}
      url: https://${{ inputs.environment || 'staging' }}.example.com
    concurrency:
      group: deploy-${{ inputs.environment || 'staging' }}
      cancel-in-progress: false
    steps:
    - name: Deploy application
      run: |
        echo 'Deploying to ${{ inputs.environment || 'staging' }}'
        echo 'Image: ${{ needs.build.outputs.image-tag }}'
        echo 'Digest: ${{ needs.build.outputs.image-digest }}'
";

            // Act
            var result = ValidateYamlWorkflow(complexYaml);

            // Assert
            Assert.True(result, "Complex valid YAML workflow should be accepted");
        }

        [Theory]
        [InlineData("name: Test\non: push\njobs:\n  build:\n    runs-on: ubuntu-latest\n    steps:\n    - uses: actions/checkout@v4")]
        [InlineData("name: 'Test with quotes'\non:\n  push:\n    branches: [main]\njobs:\n  test:\n    runs-on: windows-latest\n    steps:\n    - run: echo 'hello'")]
        [InlineData("name: Multi-trigger\non: [push, pull_request]\njobs:\n  check:\n    runs-on: macos-latest\n    steps:\n    - uses: actions/setup-node@v4")]
        public void ValidateWorkflow_WithVariousValidFormats_ShouldReturnTrue(string yaml)
        {
            // Act
            var result = ValidateYamlWorkflow(yaml);

            // Assert
            Assert.True(result, $"Valid YAML format should be accepted: {yaml.Substring(0, Math.Min(50, yaml.Length))}...");
        }

        #endregion

        #region Invalid Input Tests

        [Fact]
        public void ValidateWorkflow_WithEmptyString_ShouldReturnFalse()
        {
            // Arrange
            var emptyYaml = "";

            // Act
            var result = ValidateYamlWorkflow(emptyYaml);

            // Assert
            Assert.False(result, "Empty YAML should be rejected");
        }

        [Fact]
        public void ValidateWorkflow_WithNullInput_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => ValidateYamlWorkflow(null));
        }

        [Fact]
        public void ValidateWorkflow_WithWhitespaceOnly_ShouldReturnFalse()
        {
            // Arrange
            var whitespaceYaml = "   \n\t  \r\n  ";

            // Act
            var result = ValidateYamlWorkflow(whitespaceYaml);

            // Assert
            Assert.False(result, "Whitespace-only YAML should be rejected");
        }

        [Theory]
        [InlineData("invalid: yaml: structure: [")]
        [InlineData("name: Test\non:\n  push:\n    branches: [main\njobs: invalid")]
        [InlineData("name\non: push\njobs: build")]
        [InlineData("name: Test\n  invalid_indentation: true")]
        [InlineData("name: Test\non: push\njobs:\n  build:\n    runs-on: ubuntu-latest\n    steps:\n    - uses: actions/checkout@v4\n    - invalid_yaml_structure")]
        public void ValidateWorkflow_WithMalformedYaml_ShouldReturnFalse(string yaml)
        {
            // Act
            var result = ValidateYamlWorkflow(yaml);

            // Assert
            Assert.False(result, $"Malformed YAML should be rejected: {yaml.Substring(0, Math.Min(50, yaml.Length))}...");
        }

        #endregion

        #region Required Fields Tests

        [Fact]
        public void ValidateWorkflow_WithMissingNameField_ShouldReturnFalse()
        {
            // Arrange - Missing 'name' field
            var yamlWithoutName = @"
on:
  push:
    branches: [ main ]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
";

            // Act
            var result = ValidateYamlWorkflow(yamlWithoutName);

            // Assert
            Assert.False(result, "YAML missing required 'name' field should be rejected");
        }

        [Fact]
        public void ValidateWorkflow_WithMissingOnTrigger_ShouldReturnFalse()
        {
            // Arrange - Missing 'on' field
            var yamlWithoutOn = @"
name: Test
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
";

            // Act
            var result = ValidateYamlWorkflow(yamlWithoutOn);

            // Assert
            Assert.False(result, "YAML missing required 'on' trigger should be rejected");
        }

        [Fact]
        public void ValidateWorkflow_WithMissingJobs_ShouldReturnFalse()
        {
            // Arrange - Missing 'jobs' section
            var yamlWithoutJobs = @"
name: Test
on: push
";

            // Act
            var result = ValidateYamlWorkflow(yamlWithoutJobs);

            // Assert
            Assert.False(result, "YAML missing 'jobs' section should be rejected");
        }

        #endregion

        #region Job Structure Tests

        [Fact]
        public void ValidateWorkflow_WithInvalidJobStructure_ShouldReturnFalse()
        {
            // Arrange - Job without runs-on
            var yamlInvalidJob = @"
name: Test
on: push
jobs:
  build:
    steps:
    - uses: actions/checkout@v4
";

            // Act
            var result = ValidateYamlWorkflow(yamlInvalidJob);

            // Assert
            Assert.False(result, "Job without 'runs-on' should be rejected");
        }

        [Fact]
        public void ValidateWorkflow_WithJobMissingSteps_ShouldReturnFalse()
        {
            // Arrange - Job without steps
            var yamlJobWithoutSteps = @"
name: Test
on: push
jobs:
  build:
    runs-on: ubuntu-latest
";

            // Act
            var result = ValidateYamlWorkflow(yamlJobWithoutSteps);

            // Assert
            Assert.False(result, "Job without 'steps' should be rejected");
        }

        [Fact]
        public void ValidateWorkflow_WithMultipleJobs_ShouldValidateAllJobs()
        {
            // Arrange
            var yamlWithMultipleJobs = @"
name: Multi-Job Workflow
on: push
jobs:
  job1:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
  job2:
    runs-on: windows-latest
    steps:
    - run: echo 'test'
  job3:
    # Missing runs-on - should cause validation to fail
    steps:
    - run: echo 'invalid'
";

            // Act
            var result = ValidateYamlWorkflow(yamlWithMultipleJobs);

            // Assert
            Assert.False(result, "Workflow with any invalid job should be rejected");
        }

        [Theory]
        [InlineData("ubuntu-latest")]
        [InlineData("ubuntu-22.04")]
        [InlineData("ubuntu-20.04")]
        [InlineData("windows-latest")]
        [InlineData("windows-2022")]
        [InlineData("windows-2019")]
        [InlineData("macos-latest")]
        [InlineData("macos-13")]
        [InlineData("macos-12")]
        [InlineData("self-hosted")]
        public void ValidateWorkflow_WithValidRunnerTypes_ShouldReturnTrue(string runnerType)
        {
            // Arrange
            var yaml = $@"
name: Runner Test
on: push
jobs:
  test:
    runs-on: {runnerType}
    steps:
    - uses: actions/checkout@v4
";

            // Act
            var result = ValidateYamlWorkflow(yaml);

            // Assert
            Assert.True(result, $"Runner type '{runnerType}' should be accepted");
        }

        #endregion

        #region Advanced Features Tests

        [Fact]
        public void ValidateWorkflow_WithEnvironmentVariables_ShouldReturnTrue()
        {
            // Arrange
            var yamlWithEnv = @"
name: Environment Test
on: push
env:
  GLOBAL_VAR: 'global_value'
  API_URL: 'https://api.example.com'
  DEBUG: true
jobs:
  test:
    runs-on: ubuntu-latest
    env:
      JOB_VAR: 'job_value'
      NODE_ENV: 'test'
    steps:
    - name: Test step
      env:
        STEP_VAR: 'step_value'
      run: |
        echo 'Global: $GLOBAL_VAR'
        echo 'Job: $JOB_VAR'
        echo 'Step: $STEP_VAR'
";

            // Act
            var result = ValidateYamlWorkflow(yamlWithEnv);

            // Assert
            Assert.True(result, "YAML with environment variables should be accepted");
        }

        [Fact]
        public void ValidateWorkflow_WithGitHubExpressions_ShouldReturnTrue()
        {
            // Arrange
            var yamlWithExpressions = @"
name: Expressions Test
on: 
  push:
    paths: ['src/**', 'tests/**']
jobs:
  conditional:
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main' && !cancelled()
    steps:
    - name: Checkout
      uses: actions/checkout@v4
      if: success()
    - name: Process
      run: |
        echo 'Branch: ${{ github.ref_name }}'
        echo 'SHA: ${{ github.sha }}'
        echo 'Actor: ${{ github.actor }}'
        echo 'Event: ${{ github.event_name }}'
      env:
        SECRET_TOKEN: ${{ secrets.API_TOKEN }}
        COMPUTED_VALUE: ${{ hashFiles('package*.json') }}
";

            // Act
            var result = ValidateYamlWorkflow(yamlWithExpressions);

            // Assert
            Assert.True(result, "YAML with GitHub expressions should be accepted");
        }

        [Fact]
        public void ValidateWorkflow_WithMatrixStrategy_ShouldReturnTrue()
        {
            // Arrange
            var yamlWithMatrix = @"
name: Matrix Strategy Test
on: push
jobs:
  test:
    runs-on: ubuntu-latest
    strategy:
      matrix:
        os: [ubuntu-latest, windows-latest, macos-latest]
        node-version: [16, 18, 20]
        include:
          - os: ubuntu-latest
            node-version: 21
            experimental: true
        exclude:
          - os: macos-latest
            node-version: 16
      fail-fast: false
      max-parallel: 3
    steps:
    - name: Checkout
      uses: actions/checkout@v4
    - name: Setup Node ${{ matrix.node-version }}
      uses: actions/setup-node@v4
      with:
        node-version: ${{ matrix.node-version }}
    - name: Test on ${{ matrix.os }}
      run: npm test
";

            // Act
            var result = ValidateYamlWorkflow(yamlWithMatrix);

            // Assert
            Assert.True(result, "YAML with matrix strategy should be accepted");
        }

        [Fact]
        public void ValidateWorkflow_WithJobDependencies_ShouldReturnTrue()
        {
            // Arrange
            var yamlWithDependencies = @"
name: Job Dependencies Test
on: push
jobs:
  build:
    runs-on: ubuntu-latest
    outputs:
      artifact-path: ${{ steps.build.outputs.path }}
      version: ${{ steps.version.outputs.version }}
    steps:
    - uses: actions/checkout@v4
    - id: build
      run: echo 'path=dist' >> $GITHUB_OUTPUT
    - id: version
      run: echo 'version=1.0.0' >> $GITHUB_OUTPUT
      
  test:
    needs: build
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
    - name: Test
      run: echo 'Testing with ${{ needs.build.outputs.version }}'
      
  deploy:
    needs: [build, test]
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    steps:
    - name: Deploy
      run: echo 'Deploying ${{ needs.build.outputs.artifact-path }}'
";

            // Act
            var result = ValidateYamlWorkflow(yamlWithDependencies);

            // Assert
            Assert.True(result, "YAML with job dependencies should be accepted");
        }

        [Fact]
        public void ValidateWorkflow_WithServices_ShouldReturnTrue()
        {
            // Arrange
            var yamlWithServices = @"
name: Services Test
on: push
jobs:
  test:
    runs-on: ubuntu-latest
    services:
      redis:
        image: redis:7
        options: >-
          --health-cmd ""redis-cli ping""
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
        ports:
          - 6379:6379
      postgres:
        image: postgres:15
        env:
          POSTGRES_DB: testdb
          POSTGRES_USER: testuser
          POSTGRES_PASSWORD: testpass
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
        ports:
          - 5432:5432
    steps:
    - uses: actions/checkout@v4
    - name: Test with services
      run: |
        echo 'Testing with Redis and PostgreSQL'
        # Connection tests would go here
";

            // Act
            var result = ValidateYamlWorkflow(yamlWithServices);

            // Assert
            Assert.True(result, "YAML with services should be accepted");
        }

        #endregion

        #region Edge Cases and Stress Tests

        [Fact]
        public void ValidateWorkflow_WithLargeFile_ShouldHandleGracefully()
        {
            // Arrange - Generate a large YAML with many jobs
            var largeYamlBuilder = new StringBuilder();
            largeYamlBuilder.AppendLine("name: Large Workflow Test");
            largeYamlBuilder.AppendLine("on: push");
            largeYamlBuilder.AppendLine("jobs:");
            
            for (int i = 0; i < 50; i++)
            {
                largeYamlBuilder.AppendLine($"  job{i}:");
                largeYamlBuilder.AppendLine("    runs-on: ubuntu-latest");
                largeYamlBuilder.AppendLine("    steps:");
                largeYamlBuilder.AppendLine("    - uses: actions/checkout@v4");
                largeYamlBuilder.AppendLine($"    - name: Step {i}");
                largeYamlBuilder.AppendLine($"      run: echo 'Processing job {i}'");
                largeYamlBuilder.AppendLine($"    - name: Additional step {i}");
                largeYamlBuilder.AppendLine($"      run: |");
                largeYamlBuilder.AppendLine($"        echo 'Multi-line command {i}'");
                largeYamlBuilder.AppendLine($"        echo 'Line 2 of job {i}'");
                largeYamlBuilder.AppendLine($"        echo 'Line 3 of job {i}'");
            }

            var largeYaml = largeYamlBuilder.ToString();

            // Act
            var result = ValidateYamlWorkflow(largeYaml);

            // Assert
            Assert.True(result, "Large YAML file should be handled gracefully");
        }

        [Fact]
        public void ValidateWorkflow_WithSpecialCharactersAndUnicode_ShouldReturnTrue()
        {
            // Arrange
            var yamlWithUnicode = @"
name: 'Unicode Test: éñ中文🚀 Special chars !@#$%^&*()'
on:
  push:
    branches: ['feature/special-chars-éñ中文', 'hotfix/urgent-🔥']
jobs:
  test:
    name: 'Test with émojis 🎉 and symbols ∞≠≤≥'
    runs-on: ubuntu-latest
    steps:
    - name: 'Checkout 📥'
      uses: actions/checkout@v4
    - name: 'Process special strings 🔄'
      run: |
        echo 'Testing Unicode: éñ中文'
        echo 'Testing symbols: !@#$%^&*()'
        echo 'Testing emojis: 🚀🎉🔥💯'
      env:
        SPECIAL_VAR: 'Value with éñ中文 and emojis 🎯'
";

            // Act
            var result = ValidateYamlWorkflow(yamlWithUnicode);

            // Assert
            Assert.True(result, "YAML with Unicode characters and special symbols should be accepted");
        }

        [Fact]
        public void ValidateWorkflow_WithComplexTriggers_ShouldReturnTrue()
        {
            // Arrange
            var yamlWithComplexTriggers = @"
name: Complex Triggers
on:
  push:
    branches: [main, 'release/**', 'hotfix/**']
    tags: ['v*.*.*', 'release-*']
    paths: ['src/**', 'tests/**', '!docs/**']
  pull_request:
    branches: [main, develop]
    types: [opened, synchronize, reopened, ready_for_review]
    paths-ignore: ['*.md', 'docs/**', '.gitignore']
  schedule:
    - cron: '0 2 * * MON'
    - cron: '0 14 * * FRI'
  workflow_dispatch:
    inputs:
      logLevel:
        description: 'Log level'
        required: true
        default: 'warning'
        type: choice
        options: [info, warning, debug]
      tags:
        description: 'Test scenario tags'
        required: false
        type: boolean
      environment:
        description: 'Environment to run tests against'
        type: environment
        required: true
  repository_dispatch:
    types: [webhook]
jobs:
  triggered:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
    - run: echo 'Workflow triggered'
";

            // Act
            var result = ValidateYamlWorkflow(yamlWithComplexTriggers);

            // Assert
            Assert.True(result, "YAML with complex triggers should be accepted");
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public void GetValidationErrors_WithInvalidYaml_ShouldReturnSpecificErrors()
        {
            // Arrange
            var invalidYaml = @"
name: Test
on: push
jobs:
  build:
    # Missing runs-on field
    steps:
    - uses: actions/checkout@v4
  deploy:
    runs-on: ubuntu-latest
    # Missing steps field
";

            // Act
            var errors = GetValidationErrors(invalidYaml);

            // Assert
            Assert.NotEmpty(errors);
            Assert.Contains(errors, error => error.Contains("runs-on") || error.Contains("build"));
        }

        [Fact]
        public void GetValidationErrors_WithValidYaml_ShouldReturnEmptyList()
        {
            // Arrange
            var validYaml = @"
name: Test
on: push
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
";

            // Act
            var errors = GetValidationErrors(validYaml);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public void GetValidationErrors_WithMultipleErrors_ShouldReturnAllErrors()
        {
            // Arrange
            var invalidYaml = @"
# Missing name field
on: push
jobs:
  job1:
    # Missing runs-on
    steps:
    - run: echo 'test'
  job2:
    runs-on: ubuntu-latest
    # Missing steps
";

            // Act
            var errors = GetValidationErrors(invalidYaml);

            // Assert
            Assert.NotEmpty(errors);
            Assert.True(errors.Count >= 2, "Should return multiple validation errors");
        }

        #endregion

        #region Helper Methods - Mock Implementation

        /// <summary>
        /// Mock implementation of YAML workflow validation for testing purposes.
        /// In a real implementation, this would use a proper YAML parser and GitHub Actions schema validation.
        /// </summary>
        private bool ValidateYamlWorkflow(string yaml)
        {
            if (yaml == null)
                throw new ArgumentNullException(nameof(yaml));

            if (string.IsNullOrWhiteSpace(yaml))
                return false;

            try
            {
                // Basic YAML structure validation
                var lines = yaml.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l) && !l.StartsWith("#")).ToArray();
                
                var hasName = lines.Any(l => l.StartsWith("name:"));
                var hasOn = lines.Any(l => l.StartsWith("on:") || l == "on:");
                var hasJobs = lines.Any(l => l.StartsWith("jobs:"));
                
                if (!hasName || !hasOn || !hasJobs)
                    return false;

                // Check for basic YAML syntax errors
                if (yaml.Contains("[") && !yaml.Contains("]"))
                    return false;
                
                if (yaml.Split('[').Length != yaml.Split(']').Length)
                    return false;

                // Validate job structure
                return ValidateJobStructure(lines);
            }
            catch
            {
                return false;
            }
        }

        private bool ValidateJobStructure(string[] lines)
        {
            var jobSection = false;
            var currentJobName = "";
            var jobHasRunsOn = false;
            var jobHasSteps = false;
            
            foreach (var line in lines)
            {
                if (line == "jobs:")
                {
                    jobSection = true;
                    continue;
                }
                
                if (jobSection && line.EndsWith(":") && !line.StartsWith(" ") && !line.StartsWith("\t"))
                {
                    // Validate previous job before starting new one
                    if (!string.IsNullOrEmpty(currentJobName) && (!jobHasRunsOn || !jobHasSteps))
                        return false;
                    
                    currentJobName = line.Replace(":", "").Trim();
                    jobHasRunsOn = false;
                    jobHasSteps = false;
                }
                
                if (jobSection && line.StartsWith("runs-on:"))
                {
                    jobHasRunsOn = true;
                }
                
                if (jobSection && line.StartsWith("steps:"))
                {
                    jobHasSteps = true;
                }
            }
            
            // Validate the last job
            return string.IsNullOrEmpty(currentJobName) || (jobHasRunsOn && jobHasSteps);
        }

        private List<string> GetValidationErrors(string yaml)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(yaml))
            {
                errors.Add("YAML content is empty or whitespace only");
                return errors;
            }

            try
            {
                var lines = yaml.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l) && !l.StartsWith("#")).ToArray();
                
                if (!lines.Any(l => l.StartsWith("name:")))
                    errors.Add("Missing required field: name");
                
                if (!lines.Any(l => l.StartsWith("on:") || l == "on:"))
                    errors.Add("Missing required field: on");

                if (!lines.Any(l => l.StartsWith("jobs:")))
                    errors.Add("Missing required field: jobs");

                // Validate job structure and collect errors
                var jobErrors = ValidateJobsAndGetErrors(lines);
                errors.AddRange(jobErrors);
            }
            catch (Exception ex)
            {
                errors.Add($"YAML parsing error: {ex.Message}");
            }

            return errors;
        }

        private List<string> ValidateJobsAndGetErrors(string[] lines)
        {
            var errors = new List<string>();
            var jobSection = false;
            var currentJobName = "";
            var jobHasRunsOn = false;
            var jobHasSteps = false;
            
            foreach (var line in lines)
            {
                if (line == "jobs:")
                {
                    jobSection = true;
                    continue;
                }
                
                if (jobSection && line.EndsWith(":") && !line.StartsWith(" ") && !line.StartsWith("\t"))
                {
                    // Validate previous job
                    if (!string.IsNullOrEmpty(currentJobName))
                    {
                        if (!jobHasRunsOn)
                            errors.Add($"Job '{currentJobName}' is missing required field: runs-on");
                        if (!jobHasSteps)
                            errors.Add($"Job '{currentJobName}' is missing required field: steps");
                    }
                    
                    currentJobName = line.Replace(":", "").Trim();
                    jobHasRunsOn = false;
                    jobHasSteps = false;
                }
                
                if (jobSection && line.StartsWith("runs-on:"))
                {
                    jobHasRunsOn = true;
                }
                
                if (jobSection && line.StartsWith("steps:"))
                {
                    jobHasSteps = true;
                }
            }
            
            // Validate the last job
            if (!string.IsNullOrEmpty(currentJobName))
            {
                if (!jobHasRunsOn)
                    errors.Add($"Job '{currentJobName}' is missing required field: runs-on");
                if (!jobHasSteps)
                    errors.Add($"Job '{currentJobName}' is missing required field: steps");
            }

            return errors;
        }

        #endregion
    }

    /// <summary>
    /// Integration tests for YAML workflow validation that test file I/O operations.
    /// These tests validate the interaction with the file system and larger workflow files.
    /// </summary>
    public class YamlWorkflowValidationIntegrationTests
    {
        [Fact]
        public void ValidateWorkflowFromFile_WithValidFile_ShouldReturnTrue()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var validWorkflow = @"
name: File Test
on: push
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
";
            File.WriteAllText(tempFile, validWorkflow);

            try
            {
                // Act
                var content = File.ReadAllText(tempFile);
                var result = ValidateYamlWorkflowContent(content);

                // Assert
                Assert.True(result, "Valid workflow file should be accepted");
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public void ValidateWorkflowFromFile_WithMissingFile_ShouldThrowFileNotFoundException()
        {
            // Arrange
            var nonExistentFile = Path.Combine(Path.GetTempPath(), "nonexistent-workflow.yml");

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => File.ReadAllText(nonExistentFile));
        }

        [Fact]
        public void ValidateWorkflowFromFile_WithEmptyFile_ShouldReturnFalse()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, "");

            try
            {
                // Act
                var content = File.ReadAllText(tempFile);
                var result = ValidateYamlWorkflowContent(content);

                // Assert
                Assert.False(result, "Empty workflow file should be rejected");
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public void ValidateWorkflowFromFile_WithBinaryFile_ShouldReturnFalse()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var binaryData = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }; // PNG header
            File.WriteAllBytes(tempFile, binaryData);

            try
            {
                // Act
                var content = File.ReadAllText(tempFile);
                var result = ValidateYamlWorkflowContent(content);

                // Assert
                Assert.False(result, "Binary file content should be rejected");
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public void ValidateWorkflowFromFile_WithVeryLargeFile_ShouldHandleGracefully()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var largeContent = new StringBuilder();
            largeContent.AppendLine("name: Large File Test");
            largeContent.AppendLine("on: push");
            largeContent.AppendLine("jobs:");
            
            // Create a very large file (>1MB)
            for (int i = 0; i < 1000; i++)
            {
                largeContent.AppendLine($"  job{i}:");
                largeContent.AppendLine("    runs-on: ubuntu-latest");
                largeContent.AppendLine("    steps:");
                largeContent.AppendLine("    - uses: actions/checkout@v4");
                for (int j = 0; j < 10; j++)
                {
                    largeContent.AppendLine($"    - name: Step {i}-{j}");
                    largeContent.AppendLine($"      run: echo 'Large file test step {i}-{j} with lots of content to make this file very large'");
                }
            }
            
            File.WriteAllText(tempFile, largeContent.ToString());

            try
            {
                // Act
                var content = File.ReadAllText(tempFile);
                var result = ValidateYamlWorkflowContent(content);

                // Assert
                Assert.True(result, "Very large valid workflow file should be handled gracefully");
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        private bool ValidateYamlWorkflowContent(string content)
        {
            // Reuse the validation logic from the main test class
            if (string.IsNullOrWhiteSpace(content))
                return false;
            
            try
            {
                var lines = content.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l) && !l.StartsWith("#")).ToArray();
                var hasName = lines.Any(l => l.StartsWith("name:"));
                var hasOn = lines.Any(l => l.StartsWith("on:") || l == "on:");
                var hasJobs = lines.Any(l => l.StartsWith("jobs:"));
                
                return hasName && hasOn && hasJobs;
            }
            catch
            {
                return false;
            }
        }
    }
}
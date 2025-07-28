// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using System.Diagnostics;
using System.IO;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace DotNetApiDiff.Tests.Integration;

/// <summary>
/// Integration tests for CLI workflows using the actual executable
/// </summary>
[Trait("Category", "Integration")]
public class CliWorkflowTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _testDataPath;
    private readonly string _tempOutputPath;
    private readonly string? _executablePath;

    public CliWorkflowTests(ITestOutputHelper output)
    {
        _output = output;
        _testDataPath = Path.Combine(Directory.GetCurrentDirectory(), "TestData");
        _tempOutputPath = Path.Combine(Path.GetTempPath(), "DotNetApiDiff.CliTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempOutputPath);

        // Find the executable path
        _executablePath = FindExecutablePath();
    }

    private string? FindExecutablePath()
    {
        var currentDir = Directory.GetCurrentDirectory();

        // Look for the built executable in common locations
        var possiblePaths = new[]
        {
            Path.Combine(currentDir, "..", "..", "..", "..", "..", "src", "DotNetApiDiff", "bin", "Debug", "net8.0", "DotNetApiDiff.exe"),
            Path.Combine(currentDir, "..", "..", "..", "..", "..", "src", "DotNetApiDiff", "bin", "Debug", "net8.0", "DotNetApiDiff"),
            Path.Combine(currentDir, "..", "..", "..", "..", "..", "src", "DotNetApiDiff", "bin", "Release", "net8.0", "DotNetApiDiff.exe"),
            Path.Combine(currentDir, "..", "..", "..", "..", "..", "src", "DotNetApiDiff", "bin", "Release", "net8.0", "DotNetApiDiff")
        };

        foreach (var path in possiblePaths)
        {
            var fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        // Check if the project file exists for dotnet run
        var projectPath = Path.Combine(currentDir, "..", "..", "..", "..", "..", "src", "DotNetApiDiff", "DotNetApiDiff.csproj");
        var fullProjectPath = Path.GetFullPath(projectPath);
        if (File.Exists(fullProjectPath))
        {
            return "dotnet";
        }

        return null;
    }

    private ProcessResult RunCliCommand(string arguments, int expectedExitCode = -1)
    {
        var processInfo = new ProcessStartInfo
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        if (_executablePath == "dotnet")
        {
            processInfo.FileName = "dotnet";
            var projectPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "src", "DotNetApiDiff", "DotNetApiDiff.csproj");
            processInfo.Arguments = $"run --project \"{projectPath}\" -- {arguments}";
        }
        else
        {
            processInfo.FileName = _executablePath;
            processInfo.Arguments = arguments;
        }

        using var process = new Process { StartInfo = processInfo };

        process.Start();

        // Read output synchronously to avoid StringBuilder issues in CI
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();

        process.WaitForExit();

        var result = new ProcessResult
        {
            ExitCode = process.ExitCode,
            StandardOutput = output,
            StandardError = error
        };

        _output.WriteLine($"Process exited with code: {result.ExitCode}");
        if (!string.IsNullOrEmpty(output))
        {
            _output.WriteLine($"STDOUT: {output}");
        }
        if (!string.IsNullOrEmpty(error))
        {
            _output.WriteLine($"STDERR: {error}");
        }

        if (expectedExitCode >= 0)
        {
            Assert.Equal(expectedExitCode, result.ExitCode);
        }

        return result;
    }

    [Fact]
    public void CliWorkflow_WithValidAssemblies_ShouldSucceed()
    {
        // Arrange
        var sourceAssembly = Path.Combine(_testDataPath, "TestAssemblyV1.dll");
        var targetAssembly = Path.Combine(_testDataPath, "TestAssemblyV2.dll");

        // Fail test if prerequisites are not met
        Assert.NotNull(_executablePath);
        Assert.True(File.Exists(sourceAssembly), $"Source test assembly not found: {sourceAssembly}");
        Assert.True(File.Exists(targetAssembly), $"Target test assembly not found: {targetAssembly}");

        var arguments = $"compare \"{sourceAssembly}\" \"{targetAssembly}\" --output console";

        // Act
        var result = RunCliCommand(arguments);

        // Assert
        // Exit code 2 is expected when there are breaking changes, which is normal for our test assemblies
        Assert.True(result.ExitCode == 0 || result.ExitCode == 2, $"CLI should execute successfully. Exit code: {result.ExitCode}");
        Assert.False(string.IsNullOrEmpty(result.StandardOutput), "Should produce output");
    }

    [Fact]
    public void CliWorkflow_WithConfigFile_ShouldApplyConfiguration()
    {
        // Arrange
        var sourceAssembly = Path.Combine(_testDataPath, "TestAssemblyV1.dll");
        var targetAssembly = Path.Combine(_testDataPath, "TestAssemblyV2.dll");
        var configFile = Path.Combine(_testDataPath, "config-lenient-changes.json");

        // Fail test if prerequisites are not met
        Assert.NotNull(_executablePath);
        Assert.True(File.Exists(sourceAssembly), $"Source test assembly not found: {sourceAssembly}");
        Assert.True(File.Exists(targetAssembly), $"Target test assembly not found: {targetAssembly}");
        Assert.True(File.Exists(configFile), $"Config file not found: {configFile}");

        var arguments = $"compare \"{sourceAssembly}\" \"{targetAssembly}\" --config \"{configFile}\" --output json";

        // Act
        var result = RunCliCommand(arguments);

        // Assert
        // Exit code 2 is expected when there are breaking changes, which is normal for our test assemblies
        Assert.True(result.ExitCode == 0 || result.ExitCode == 2, $"CLI should execute successfully with config. Exit code: {result.ExitCode}");
        Assert.False(string.IsNullOrEmpty(result.StandardOutput), "Should produce JSON output");
    }

    [Fact]
    public void CliWorkflow_WithNonExistentSourceAssembly_ShouldFail()
    {
        // Arrange
        var sourceAssembly = Path.Combine(_testDataPath, "non-existent.dll");
        var targetAssembly = Path.Combine(_testDataPath, "TestAssemblyV2.dll");

        // Fail test if prerequisites are not met
        Assert.NotNull(_executablePath);
        Assert.True(File.Exists(targetAssembly), $"Target test assembly not found: {targetAssembly}");

        var arguments = $"compare \"{sourceAssembly}\" \"{targetAssembly}\"";

        // Act
        var result = RunCliCommand(arguments);

        // Assert
        Assert.NotEqual(0, result.ExitCode);
        var combinedOutput = result.StandardOutput + result.StandardError;
        Assert.True(combinedOutput.Contains("not found") || combinedOutput.Contains("Source assembly file not found") || combinedOutput.Contains("non-existent.dll"),
            $"Should indicate file not found. Combined output: {combinedOutput}");
    }

    [Fact]
    public void CliWorkflow_WithNonExistentTargetAssembly_ShouldFail()
    {
        // Arrange
        var sourceAssembly = Path.Combine(_testDataPath, "TestAssemblyV1.dll");
        var targetAssembly = Path.Combine(_testDataPath, "non-existent.dll");

        // Fail test if prerequisites are not met
        Assert.NotNull(_executablePath);
        Assert.True(File.Exists(sourceAssembly), $"Source test assembly not found: {sourceAssembly}");

        var arguments = $"compare \"{sourceAssembly}\" \"{targetAssembly}\"";

        // Act
        var result = RunCliCommand(arguments);

        // Assert
        Assert.NotEqual(0, result.ExitCode);
        var combinedOutput = result.StandardOutput + result.StandardError;
        Assert.True(combinedOutput.Contains("not found") || combinedOutput.Contains("Target assembly file not found") || combinedOutput.Contains("non-existent.dll"),
            $"Should indicate file not found. Combined output: {combinedOutput}");
    }

    [Fact]
    public void CliWorkflow_WithNonExistentConfigFile_ShouldFail()
    {
        // Arrange
        var sourceAssembly = Path.Combine(_testDataPath, "TestAssemblyV1.dll");
        var targetAssembly = Path.Combine(_testDataPath, "TestAssemblyV2.dll");
        var configFile = Path.Combine(_testDataPath, "non-existent-config.json");

        // Fail test if prerequisites are not met
        Assert.NotNull(_executablePath);
        Assert.True(File.Exists(sourceAssembly), $"Source test assembly not found: {sourceAssembly}");
        Assert.True(File.Exists(targetAssembly), $"Target test assembly not found: {targetAssembly}");

        var arguments = $"compare \"{sourceAssembly}\" \"{targetAssembly}\" --config \"{configFile}\"";

        // Act
        var result = RunCliCommand(arguments);

        // Assert
        Assert.NotEqual(0, result.ExitCode);
        var combinedOutput = result.StandardOutput + result.StandardError;
        Assert.True(combinedOutput.Contains("not found") || combinedOutput.Contains("Configuration file not found"),
            $"Should indicate config file not found. Combined output: {combinedOutput}");
    }

    [Fact]
    public void CliWorkflow_WithMalformedConfigFile_ShouldFail()
    {
        // Arrange
        var sourceAssembly = Path.Combine(_testDataPath, "TestAssemblyV1.dll");
        var targetAssembly = Path.Combine(_testDataPath, "TestAssemblyV2.dll");
        var configFile = Path.Combine(_testDataPath, "config-malformed.json");

        // Fail test if prerequisites are not met
        Assert.NotNull(_executablePath);
        Assert.True(File.Exists(sourceAssembly), $"Source test assembly not found: {sourceAssembly}");
        Assert.True(File.Exists(targetAssembly), $"Target test assembly not found: {targetAssembly}");
        Assert.True(File.Exists(configFile), $"Malformed config file not found: {configFile}");

        var arguments = $"compare \"{sourceAssembly}\" \"{targetAssembly}\" --config \"{configFile}\"";

        // Act
        var result = RunCliCommand(arguments);

        // Assert
        Assert.NotEqual(0, result.ExitCode);
        var combinedOutput = result.StandardOutput + result.StandardError;
        Assert.True(combinedOutput.Contains("JSON") || combinedOutput.Contains("configuration") || combinedOutput.Contains("malformed"),
            $"Should indicate JSON/configuration error. Combined output: {combinedOutput}");
    }

    [Theory]
    [InlineData("console")]
    [InlineData("json")]
    [InlineData("markdown")]
    public void CliWorkflow_WithDifferentOutputFormats_ShouldSucceed(string outputFormat)
    {
        // Arrange
        var sourceAssembly = Path.Combine(_testDataPath, "TestAssemblyV1.dll");
        var targetAssembly = Path.Combine(_testDataPath, "TestAssemblyV2.dll");

        // Fail test if prerequisites are not met
        Assert.NotNull(_executablePath);
        Assert.True(File.Exists(sourceAssembly), $"Source test assembly not found: {sourceAssembly}");
        Assert.True(File.Exists(targetAssembly), $"Target test assembly not found: {targetAssembly}");

        var arguments = $"compare \"{sourceAssembly}\" \"{targetAssembly}\" --output {outputFormat}";

        // Act
        var result = RunCliCommand(arguments);

        // Assert
        // Exit code 2 is expected when there are breaking changes, which is normal for our test assemblies
        Assert.True(result.ExitCode == 0 || result.ExitCode == 2, $"CLI should execute successfully with {outputFormat} format. Exit code: {result.ExitCode}");
        Assert.False(string.IsNullOrEmpty(result.StandardOutput), $"Should produce {outputFormat} output");

        // Verify output format specific content
        if (outputFormat == "json")
        {
            Assert.Contains("{", result.StandardOutput);
            Assert.Contains("}", result.StandardOutput);
        }
        else if (outputFormat == "markdown")
        {
            Assert.Contains("#", result.StandardOutput);
        }
    }

    [Fact]
    public void CliWorkflow_WithInvalidOutputFormat_ShouldFail()
    {
        // Arrange
        var sourceAssembly = Path.Combine(_testDataPath, "TestAssemblyV1.dll");
        var targetAssembly = Path.Combine(_testDataPath, "TestAssemblyV2.dll");

        // Fail test if prerequisites are not met
        Assert.NotNull(_executablePath);
        Assert.True(File.Exists(sourceAssembly), $"Source test assembly not found: {sourceAssembly}");
        Assert.True(File.Exists(targetAssembly), $"Target test assembly not found: {targetAssembly}");

        var arguments = $"compare \"{sourceAssembly}\" \"{targetAssembly}\" --output invalid_format";

        // Act
        var result = RunCliCommand(arguments);

        // Assert
        Assert.NotEqual(0, result.ExitCode);
        var combinedOutput = result.StandardOutput + result.StandardError;
        Assert.True(combinedOutput.Contains("Invalid output format") || combinedOutput.Contains("invalid_format"),
            $"Should indicate invalid output format. Combined output: {combinedOutput}");
    }

    [Fact]
    public void CliWorkflow_WithNamespaceFiltering_ShouldApplyFilters()
    {
        // Arrange
        var sourceAssembly = Path.Combine(_testDataPath, "TestAssemblyV1.dll");
        var targetAssembly = Path.Combine(_testDataPath, "TestAssemblyV2.dll");

        // Fail test if prerequisites are not met
        Assert.NotNull(_executablePath);
        Assert.True(File.Exists(sourceAssembly), $"Source test assembly not found: {sourceAssembly}");
        Assert.True(File.Exists(targetAssembly), $"Target test assembly not found: {targetAssembly}");

        var arguments = $"compare \"{sourceAssembly}\" \"{targetAssembly}\" --filter System.Text --output console";

        // Act
        var result = RunCliCommand(arguments);

        // Assert
        // Exit code 2 is expected when there are breaking changes, which is normal for our test assemblies
        Assert.True(result.ExitCode == 0 || result.ExitCode == 2, $"CLI should succeed with namespace filtering. Exit code: {result.ExitCode}");
        Assert.False(string.IsNullOrEmpty(result.StandardOutput), "Should produce filtered output");
    }

    [Fact]
    public void CliWorkflow_WithVerboseOutput_ShouldProduceDetailedLogs()
    {
        // Arrange
        var sourceAssembly = Path.Combine(_testDataPath, "TestAssemblyV1.dll");
        var targetAssembly = Path.Combine(_testDataPath, "TestAssemblyV2.dll");

        // Fail test if prerequisites are not met
        Assert.NotNull(_executablePath);
        Assert.True(File.Exists(sourceAssembly), $"Source test assembly not found: {sourceAssembly}");
        Assert.True(File.Exists(targetAssembly), $"Target test assembly not found: {targetAssembly}");

        var arguments = $"compare \"{sourceAssembly}\" \"{targetAssembly}\" --verbose --output console";

        // Act
        var result = RunCliCommand(arguments);

        // Assert
        // Exit code 2 is expected when there are breaking changes, which is normal for our test assemblies
        Assert.True(result.ExitCode == 0 || result.ExitCode == 2, $"CLI should succeed with verbose output. Exit code: {result.ExitCode}");
        Assert.False(string.IsNullOrEmpty(result.StandardOutput), "Should produce verbose output");
    }

    [Fact]
    public void CliWorkflow_WithNoColorOption_ShouldDisableColors()
    {
        // Arrange
        var sourceAssembly = Path.Combine(_testDataPath, "TestAssemblyV1.dll");
        var targetAssembly = Path.Combine(_testDataPath, "TestAssemblyV2.dll");

        // Fail test if prerequisites are not met
        Assert.NotNull(_executablePath);
        Assert.True(File.Exists(sourceAssembly), $"Source test assembly not found: {sourceAssembly}");
        Assert.True(File.Exists(targetAssembly), $"Target test assembly not found: {targetAssembly}");

        var arguments = $"compare \"{sourceAssembly}\" \"{targetAssembly}\" --no-color --output console";

        // Act
        var result = RunCliCommand(arguments);

        // Assert
        // Exit code 2 is expected when there are breaking changes, which is normal for our test assemblies
        Assert.True(result.ExitCode == 0 || result.ExitCode == 2, $"CLI should succeed with no-color option. Exit code: {result.ExitCode}");
        Assert.False(string.IsNullOrEmpty(result.StandardOutput), "Should produce output without colors");
    }

    public void Dispose()
    {
        // Clean up temporary files
        if (Directory.Exists(_tempOutputPath))
        {
            try
            {
                Directory.Delete(_tempOutputPath, true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }

    private class ProcessResult
    {
        public int ExitCode { get; set; }
        public string StandardOutput { get; set; } = string.Empty;
        public string StandardError { get; set; } = string.Empty;
    }
}

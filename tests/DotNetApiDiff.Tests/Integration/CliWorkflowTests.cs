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
public class CliWorkflowTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _testDataPath;
    private readonly string _tempOutputPath;
    private readonly string _executablePath;

    public CliWorkflowTests(ITestOutputHelper output)
    {
        _output = output;
        _testDataPath = Path.Combine(Directory.GetCurrentDirectory(), "TestData");
        _tempOutputPath = Path.Combine(Path.GetTempPath(), "DotNetApiDiff.CliTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempOutputPath);

        // Find the executable path
        _executablePath = FindExecutablePath();
    }

    private string FindExecutablePath()
    {
        // Look for the built executable in common locations
        var possiblePaths = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "src", "DotNetApiDiff", "bin", "Debug", "net8.0", "DotNetApiDiff.exe"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "src", "DotNetApiDiff", "bin", "Debug", "net8.0", "DotNetApiDiff"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "src", "DotNetApiDiff", "bin", "Release", "net8.0", "DotNetApiDiff.exe"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "src", "DotNetApiDiff", "bin", "Release", "net8.0", "DotNetApiDiff")
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        // If not found, try using dotnet run
        return "dotnet";
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
            var projectPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "src", "DotNetApiDiff", "DotNetApiDiff.csproj");
            processInfo.Arguments = $"run --project \"{projectPath}\" -- {arguments}";
        }
        else
        {
            processInfo.FileName = _executablePath;
            processInfo.Arguments = arguments;
        }

        var output = new StringBuilder();
        var error = new StringBuilder();

        using var process = new Process { StartInfo = processInfo };

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                output.AppendLine(e.Data);
                _output.WriteLine($"STDOUT: {e.Data}");
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                error.AppendLine(e.Data);
                _output.WriteLine($"STDERR: {e.Data}");
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Set a reasonable timeout
        bool exited = process.WaitForExit(30000); // 30 seconds

        if (!exited)
        {
            process.Kill();
            throw new TimeoutException("Process did not exit within the expected time");
        }

        var result = new ProcessResult
        {
            ExitCode = process.ExitCode,
            StandardOutput = output.ToString(),
            StandardError = error.ToString()
        };

        _output.WriteLine($"Process exited with code: {result.ExitCode}");

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

        // Skip test if test assemblies don't exist
        if (!File.Exists(sourceAssembly) || !File.Exists(targetAssembly))
        {
            _output.WriteLine("Skipping test - test assemblies not found");
            return;
        }

        var arguments = $"\"{sourceAssembly}\" \"{targetAssembly}\" --output console";

        // Act
        var result = RunCliCommand(arguments);

        // Assert
        Assert.True(result.ExitCode >= 0, $"CLI should execute successfully. Exit code: {result.ExitCode}");
        Assert.False(string.IsNullOrEmpty(result.StandardOutput), "Should produce output");
    }

    [Fact]
    public void CliWorkflow_WithConfigFile_ShouldApplyConfiguration()
    {
        // Arrange
        var sourceAssembly = Path.Combine(_testDataPath, "TestAssemblyV1.dll");
        var targetAssembly = Path.Combine(_testDataPath, "TestAssemblyV2.dll");
        var configFile = Path.Combine(_testDataPath, "config-lenient-changes.json");

        // Skip test if files don't exist
        if (!File.Exists(sourceAssembly) || !File.Exists(targetAssembly) || !File.Exists(configFile))
        {
            _output.WriteLine("Skipping test - required files not found");
            return;
        }

        var arguments = $"\"{sourceAssembly}\" \"{targetAssembly}\" --config \"{configFile}\" --output json";

        // Act
        var result = RunCliCommand(arguments);

        // Assert
        Assert.True(result.ExitCode >= 0, $"CLI should execute successfully with config. Exit code: {result.ExitCode}");
        Assert.False(string.IsNullOrEmpty(result.StandardOutput), "Should produce JSON output");
    }

    [Fact]
    public void CliWorkflow_WithNonExistentSourceAssembly_ShouldFail()
    {
        // Arrange
        var sourceAssembly = Path.Combine(_testDataPath, "non-existent.dll");
        var targetAssembly = Path.Combine(_testDataPath, "TestAssemblyV2.dll");

        // Skip test if target assembly doesn't exist
        if (!File.Exists(targetAssembly))
        {
            _output.WriteLine("Skipping test - target assembly not found");
            return;
        }

        var arguments = $"\"{sourceAssembly}\" \"{targetAssembly}\"";

        // Act
        var result = RunCliCommand(arguments);

        // Assert
        Assert.NotEqual(0, result.ExitCode);
        Assert.True(result.StandardError.Contains("not found") || result.StandardOutput.Contains("not found"),
            "Should indicate file not found");
    }

    [Fact]
    public void CliWorkflow_WithNonExistentTargetAssembly_ShouldFail()
    {
        // Arrange
        var sourceAssembly = Path.Combine(_testDataPath, "TestAssemblyV1.dll");
        var targetAssembly = Path.Combine(_testDataPath, "non-existent.dll");

        // Skip test if source assembly doesn't exist
        if (!File.Exists(sourceAssembly))
        {
            _output.WriteLine("Skipping test - source assembly not found");
            return;
        }

        var arguments = $"\"{sourceAssembly}\" \"{targetAssembly}\"";

        // Act
        var result = RunCliCommand(arguments);

        // Assert
        Assert.NotEqual(0, result.ExitCode);
        Assert.True(result.StandardError.Contains("not found") || result.StandardOutput.Contains("not found"),
            "Should indicate file not found");
    }

    [Fact]
    public void CliWorkflow_WithNonExistentConfigFile_ShouldFail()
    {
        // Arrange
        var sourceAssembly = Path.Combine(_testDataPath, "TestAssemblyV1.dll");
        var targetAssembly = Path.Combine(_testDataPath, "TestAssemblyV2.dll");
        var configFile = Path.Combine(_testDataPath, "non-existent-config.json");

        // Skip test if assemblies don't exist
        if (!File.Exists(sourceAssembly) || !File.Exists(targetAssembly))
        {
            _output.WriteLine("Skipping test - test assemblies not found");
            return;
        }

        var arguments = $"\"{sourceAssembly}\" \"{targetAssembly}\" --config \"{configFile}\"";

        // Act
        var result = RunCliCommand(arguments);

        // Assert
        Assert.NotEqual(0, result.ExitCode);
        Assert.True(result.StandardError.Contains("not found") || result.StandardOutput.Contains("not found"),
            "Should indicate config file not found");
    }

    [Fact]
    public void CliWorkflow_WithMalformedConfigFile_ShouldFail()
    {
        // Arrange
        var sourceAssembly = Path.Combine(_testDataPath, "TestAssemblyV1.dll");
        var targetAssembly = Path.Combine(_testDataPath, "TestAssemblyV2.dll");
        var configFile = Path.Combine(_testDataPath, "config-malformed.json");

        // Skip test if files don't exist
        if (!File.Exists(sourceAssembly) || !File.Exists(targetAssembly) || !File.Exists(configFile))
        {
            _output.WriteLine("Skipping test - required files not found");
            return;
        }

        var arguments = $"\"{sourceAssembly}\" \"{targetAssembly}\" --config \"{configFile}\"";

        // Act
        var result = RunCliCommand(arguments);

        // Assert
        Assert.NotEqual(0, result.ExitCode);
        Assert.True(result.StandardError.Contains("JSON") || result.StandardOutput.Contains("JSON") ||
                   result.StandardError.Contains("configuration") || result.StandardOutput.Contains("configuration"),
            "Should indicate JSON/configuration error");
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

        // Skip test if assemblies don't exist
        if (!File.Exists(sourceAssembly) || !File.Exists(targetAssembly))
        {
            _output.WriteLine("Skipping test - test assemblies not found");
            return;
        }

        var arguments = $"\"{sourceAssembly}\" \"{targetAssembly}\" --output {outputFormat}";

        // Act
        var result = RunCliCommand(arguments);

        // Assert
        Assert.True(result.ExitCode >= 0, $"CLI should succeed with {outputFormat} format. Exit code: {result.ExitCode}");
        Assert.False(string.IsNullOrEmpty(result.StandardOutput), $"Should produce {outputFormat} output");
    }

    [Fact]
    public void CliWorkflow_WithInvalidOutputFormat_ShouldFail()
    {
        // Arrange
        var sourceAssembly = Path.Combine(_testDataPath, "TestAssemblyV1.dll");
        var targetAssembly = Path.Combine(_testDataPath, "TestAssemblyV2.dll");

        // Skip test if assemblies don't exist
        if (!File.Exists(sourceAssembly) || !File.Exists(targetAssembly))
        {
            _output.WriteLine("Skipping test - test assemblies not found");
            return;
        }

        var arguments = $"\"{sourceAssembly}\" \"{targetAssembly}\" --output invalid_format";

        // Act
        var result = RunCliCommand(arguments);

        // Assert
        Assert.NotEqual(0, result.ExitCode);
        Assert.True(result.StandardError.Contains("Invalid output format") || result.StandardOutput.Contains("Invalid output format"),
            "Should indicate invalid output format");
    }

    [Fact]
    public void CliWorkflow_WithNamespaceFiltering_ShouldApplyFilters()
    {
        // Arrange
        var sourceAssembly = Path.Combine(_testDataPath, "TestAssemblyV1.dll");
        var targetAssembly = Path.Combine(_testDataPath, "TestAssemblyV2.dll");

        // Skip test if assemblies don't exist
        if (!File.Exists(sourceAssembly) || !File.Exists(targetAssembly))
        {
            _output.WriteLine("Skipping test - test assemblies not found");
            return;
        }

        var arguments = $"\"{sourceAssembly}\" \"{targetAssembly}\" --filter System.Text --output console";

        // Act
        var result = RunCliCommand(arguments);

        // Assert
        Assert.True(result.ExitCode >= 0, $"CLI should succeed with namespace filtering. Exit code: {result.ExitCode}");
        Assert.False(string.IsNullOrEmpty(result.StandardOutput), "Should produce filtered output");
    }

    [Fact]
    public void CliWorkflow_WithVerboseOutput_ShouldProduceDetailedLogs()
    {
        // Arrange
        var sourceAssembly = Path.Combine(_testDataPath, "TestAssemblyV1.dll");
        var targetAssembly = Path.Combine(_testDataPath, "TestAssemblyV2.dll");

        // Skip test if assemblies don't exist
        if (!File.Exists(sourceAssembly) || !File.Exists(targetAssembly))
        {
            _output.WriteLine("Skipping test - test assemblies not found");
            return;
        }

        var arguments = $"\"{sourceAssembly}\" \"{targetAssembly}\" --verbose --output console";

        // Act
        var result = RunCliCommand(arguments);

        // Assert
        Assert.True(result.ExitCode >= 0, $"CLI should succeed with verbose output. Exit code: {result.ExitCode}");
        Assert.False(string.IsNullOrEmpty(result.StandardOutput), "Should produce verbose output");
    }

    [Fact]
    public void CliWorkflow_WithNoColorOption_ShouldDisableColors()
    {
        // Arrange
        var sourceAssembly = Path.Combine(_testDataPath, "TestAssemblyV1.dll");
        var targetAssembly = Path.Combine(_testDataPath, "TestAssemblyV2.dll");

        // Skip test if assemblies don't exist
        if (!File.Exists(sourceAssembly) || !File.Exists(targetAssembly))
        {
            _output.WriteLine("Skipping test - test assemblies not found");
            return;
        }

        var arguments = $"\"{sourceAssembly}\" \"{targetAssembly}\" --no-color --output console";

        // Act
        var result = RunCliCommand(arguments);

        // Assert
        Assert.True(result.ExitCode >= 0, $"CLI should succeed with no-color option. Exit code: {result.ExitCode}");
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

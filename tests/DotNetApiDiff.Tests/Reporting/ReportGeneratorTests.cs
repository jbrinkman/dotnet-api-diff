// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using DotNetApiDiff.Interfaces;
using DotNetApiDiff.Models;
using DotNetApiDiff.Reporting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotNetApiDiff.Tests.Reporting;

public class ReportGeneratorTests
{
    private readonly Mock<ILogger<ReportGenerator>> _mockLogger;
    private readonly ReportGenerator _reportGenerator;

    public ReportGeneratorTests()
    {
        _mockLogger = new Mock<ILogger<ReportGenerator>>();

        // Create a ReportGenerator with a test-mode ConsoleFormatter
        var formatters = new Dictionary<ReportFormat, IReportFormatter>
        {
            { ReportFormat.Console, new ConsoleFormatter(testMode: true) }
        };

        _reportGenerator = new ReportGenerator(_mockLogger.Object, formatters);
    }

    [Fact]
    public void GetSupportedFormats_ReturnsConsoleFormat()
    {
        // Act
        var formats = _reportGenerator.GetSupportedFormats();

        // Assert
        Assert.Contains(ReportFormat.Console, formats);
    }

    [Fact]
    public void GenerateReport_WithConsoleFormat_ReturnsFormattedReport()
    {
        // Arrange
        var result = CreateSampleComparisonResult();

        // Act
        var report = _reportGenerator.GenerateReport(result, ReportFormat.Console);

        // Assert
        Assert.NotNull(report);
        Assert.NotEmpty(report);
        // The console formatter should include the assembly names
        Assert.Contains("source.dll", report);
        Assert.Contains("target.dll", report);
    }

    [Fact]
    public void GenerateReport_WithUnsupportedFormat_FallsBackToConsole()
    {
        // Arrange
        var result = CreateSampleComparisonResult();

        // Act
        var report = _reportGenerator.GenerateReport(result, ReportFormat.Json);

        // Assert
        Assert.NotNull(report);
        Assert.NotEmpty(report);
        // Verify that we logged a warning about falling back to console format
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("not supported, falling back to Console format")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SaveReportAsync_WritesReportToFile()
    {
        // Arrange
        var result = CreateSampleComparisonResult();
        var tempFilePath = Path.GetTempFileName();

        try
        {
            // Act
            await _reportGenerator.SaveReportAsync(result, ReportFormat.Console, tempFilePath);

            // Assert
            Assert.True(File.Exists(tempFilePath));
            var fileContent = await File.ReadAllTextAsync(tempFilePath);
            Assert.NotEmpty(fileContent);
            Assert.Contains("source.dll", fileContent);
            Assert.Contains("target.dll", fileContent);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    private ComparisonResult CreateSampleComparisonResult()
    {
        return new ComparisonResult
        {
            OldAssemblyPath = "path/to/source.dll",
            NewAssemblyPath = "path/to/target.dll",
            ComparisonTimestamp = DateTime.UtcNow,
            Differences = new List<ApiDifference>
            {
                new ApiDifference
                {
                    ChangeType = ChangeType.Added,
                    ElementType = ApiElementType.Method,
                    ElementName = "System.String.Concat(string, string)",
                    Description = "Method added",
                    IsBreakingChange = false,
                    Severity = SeverityLevel.Info,
                    NewSignature = "public static string Concat(string str1, string str2)"
                },
                new ApiDifference
                {
                    ChangeType = ChangeType.Removed,
                    ElementType = ApiElementType.Method,
                    ElementName = "System.String.OldMethod()",
                    Description = "Method removed",
                    IsBreakingChange = true,
                    Severity = SeverityLevel.Error,
                    OldSignature = "public string OldMethod()"
                },
                new ApiDifference
                {
                    ChangeType = ChangeType.Modified,
                    ElementType = ApiElementType.Property,
                    ElementName = "System.String.Length",
                    Description = "Property type changed",
                    IsBreakingChange = true,
                    Severity = SeverityLevel.Critical,
                    OldSignature = "public int Length { get; }",
                    NewSignature = "public long Length { get; }"
                }
            },
            Summary = new ComparisonSummary
            {
                AddedCount = 1,
                RemovedCount = 1,
                ModifiedCount = 1,
                BreakingChangesCount = 2
            }
        };
    }
}

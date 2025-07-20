// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using DotNetApiDiff.Models;
using DotNetApiDiff.Reporting;
using Xunit;

namespace DotNetApiDiff.Tests.Reporting;

public class ConsoleFormatterTests
{
    private readonly ConsoleFormatter _formatter;

    public ConsoleFormatterTests()
    {
        _formatter = new ConsoleFormatter(testMode: true);
    }

    [Fact]
    public void Format_WithEmptyResult_ReturnsBasicReport()
    {
        // Arrange
        var result = new ComparisonResult
        {
            OldAssemblyPath = "source.dll",
            NewAssemblyPath = "target.dll",
            ComparisonTimestamp = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        };

        // Act
        var report = _formatter.Format(result);

        // Assert
        Assert.NotNull(report);
        Assert.NotEmpty(report);
        Assert.Contains("source.dll", report);
        Assert.Contains("target.dll", report);
        Assert.Contains("2023-01-01 12:00:00", report);
        Assert.Contains("Total Differences: 0", report);
        Assert.Contains("Added: 0", report);
        Assert.Contains("Removed: 0", report);
        Assert.Contains("Modified: 0", report);
    }

    [Fact]
    public void Format_WithAddedItems_IncludesAddedSection()
    {
        // Arrange
        var result = new ComparisonResult
        {
            OldAssemblyPath = "source.dll",
            NewAssemblyPath = "target.dll",
            Differences = new List<ApiDifference>
            {
                new ApiDifference
                {
                    ChangeType = ChangeType.Added,
                    ElementType = ApiElementType.Method,
                    ElementName = "System.String.NewMethod()",
                    Description = "Method added",
                    IsBreakingChange = false,
                    Severity = SeverityLevel.Info,
                    NewSignature = "public void NewMethod()"
                }
            },
            Summary = new ComparisonSummary
            {
                AddedCount = 1
            }
        };

        // Act
        var report = _formatter.Format(result);

        // Assert
        Assert.NotNull(report);
        Assert.Contains("Added Items", report);
        Assert.Contains("System.String.NewMethod()", report);
        Assert.Contains("Method added", report);
        Assert.Contains("Added: 1", report);
    }

    [Fact]
    public void Format_WithRemovedItems_IncludesRemovedSection()
    {
        // Arrange
        var result = new ComparisonResult
        {
            OldAssemblyPath = "source.dll",
            NewAssemblyPath = "target.dll",
            Differences = new List<ApiDifference>
            {
                new ApiDifference
                {
                    ChangeType = ChangeType.Removed,
                    ElementType = ApiElementType.Method,
                    ElementName = "System.String.OldMethod()",
                    Description = "Method removed",
                    IsBreakingChange = true,
                    Severity = SeverityLevel.Error,
                    OldSignature = "public void OldMethod()"
                }
            },
            Summary = new ComparisonSummary
            {
                RemovedCount = 1,
                BreakingChangesCount = 1
            }
        };

        // Act
        var report = _formatter.Format(result);

        // Assert
        Assert.NotNull(report);
        Assert.Contains("Removed Items", report);
        Assert.Contains("System.String.OldMethod()", report);
        Assert.Contains("Method removed", report);
        Assert.Contains("Removed: 1", report);
        Assert.Contains("Breaking Changes: 1", report);
    }

    [Fact]
    public void Format_WithModifiedItems_IncludesModifiedSection()
    {
        // Arrange
        var result = new ComparisonResult
        {
            OldAssemblyPath = "source.dll",
            NewAssemblyPath = "target.dll",
            Differences = new List<ApiDifference>
            {
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
                ModifiedCount = 1,
                BreakingChangesCount = 1
            }
        };

        // Act
        var report = _formatter.Format(result);

        // Assert
        Assert.NotNull(report);
        Assert.Contains("Modified Items", report);
        Assert.Contains("System.String.Length", report);
        Assert.Contains("Property type changed", report);
        Assert.Contains("Modified: 1", report);
        Assert.Contains("Breaking Changes: 1", report);
    }

    [Fact]
    public void Format_WithBreakingChanges_IncludesBreakingChangesSection()
    {
        // Arrange
        var result = new ComparisonResult
        {
            OldAssemblyPath = "source.dll",
            NewAssemblyPath = "target.dll",
            Differences = new List<ApiDifference>
            {
                new ApiDifference
                {
                    ChangeType = ChangeType.Removed,
                    ElementType = ApiElementType.Method,
                    ElementName = "System.String.OldMethod()",
                    Description = "Method removed",
                    IsBreakingChange = true,
                    Severity = SeverityLevel.Error
                },
                new ApiDifference
                {
                    ChangeType = ChangeType.Modified,
                    ElementType = ApiElementType.Property,
                    ElementName = "System.String.Length",
                    Description = "Property type changed",
                    IsBreakingChange = true,
                    Severity = SeverityLevel.Critical
                }
            },
            Summary = new ComparisonSummary
            {
                RemovedCount = 1,
                ModifiedCount = 1,
                BreakingChangesCount = 2
            }
        };

        // Act
        var report = _formatter.Format(result);

        // Assert
        Assert.NotNull(report);
        Assert.Contains("Breaking Changes", report);
        Assert.Contains("System.String.OldMethod()", report);
        Assert.Contains("System.String.Length", report);
        Assert.Contains("Breaking Changes: 2", report);
    }

    [Fact]
    public void Format_WithMultipleChangeTypes_IncludesAllSections()
    {
        // Arrange
        var result = new ComparisonResult
        {
            OldAssemblyPath = "source.dll",
            NewAssemblyPath = "target.dll",
            Differences = new List<ApiDifference>
            {
                new ApiDifference
                {
                    ChangeType = ChangeType.Added,
                    ElementType = ApiElementType.Method,
                    ElementName = "System.String.NewMethod()",
                    Description = "Method added",
                    IsBreakingChange = false,
                    Severity = SeverityLevel.Info
                },
                new ApiDifference
                {
                    ChangeType = ChangeType.Removed,
                    ElementType = ApiElementType.Method,
                    ElementName = "System.String.OldMethod()",
                    Description = "Method removed",
                    IsBreakingChange = true,
                    Severity = SeverityLevel.Error
                },
                new ApiDifference
                {
                    ChangeType = ChangeType.Modified,
                    ElementType = ApiElementType.Property,
                    ElementName = "System.String.Length",
                    Description = "Property type changed",
                    IsBreakingChange = true,
                    Severity = SeverityLevel.Critical
                },
                new ApiDifference
                {
                    ChangeType = ChangeType.Excluded,
                    ElementType = ApiElementType.Field,
                    ElementName = "System.String._firstChar",
                    Description = "Field excluded",
                    IsBreakingChange = false,
                    Severity = SeverityLevel.Info
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

        // Act
        var report = _formatter.Format(result);

        // Assert
        Assert.NotNull(report);
        Assert.Contains("Added Items", report);
        Assert.Contains("Removed Items", report);
        Assert.Contains("Modified Items", report);
        Assert.Contains("Excluded Items", report);
        Assert.Contains("Breaking Changes", report);
        Assert.Contains("System.String.NewMethod()", report);
        Assert.Contains("System.String.OldMethod()", report);
        Assert.Contains("System.String.Length", report);
        Assert.Contains("System.String._firstChar", report);
        Assert.Contains("Added: 1", report);
        Assert.Contains("Removed: 1", report);
        Assert.Contains("Modified: 1", report);
        Assert.Contains("Breaking Changes: 2", report);
    }
}

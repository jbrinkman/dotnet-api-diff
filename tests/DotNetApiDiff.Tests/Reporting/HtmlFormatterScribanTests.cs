// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using DotNetApiDiff.Models;
using DotNetApiDiff.Models.Configuration;
using DotNetApiDiff.Reporting;
using Xunit;

namespace DotNetApiDiff.Tests.Reporting;

public class HtmlFormatterScribanTests
{
    private readonly HtmlFormatterScriban _formatter;

    public HtmlFormatterScribanTests()
    {
        _formatter = new HtmlFormatterScriban();
    }

    [Fact]
    public void Format_WithEmptyResult_ReturnsValidHtml()
    {
        // Arrange
        var result = new ComparisonResult
        {
            OldAssemblyPath = "source.dll",
            NewAssemblyPath = "target.dll",
            ComparisonTimestamp = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            Configuration = CreateDefaultConfiguration()
        };

        // Act
        var report = _formatter.Format(result);

        // Assert
        Assert.NotNull(report);
        Assert.NotEmpty(report);
        Assert.Contains("<!DOCTYPE html>", report);
        Assert.Contains("API Comparison Report", report);
        Assert.Contains("source.dll", report);
        Assert.Contains("target.dll", report);
        Assert.Contains("2023-01-01 12:00:00", report);
    }

    [Fact]
    public void Format_WithAddedItems_IncludesAddedSection()
    {
        // Arrange
        var result = new ComparisonResult
        {
            OldAssemblyPath = "source.dll",
            NewAssemblyPath = "target.dll",
            ComparisonTimestamp = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            Configuration = CreateDefaultConfiguration(),
            Differences = new List<ApiDifference>
            {
                new ApiDifference
                {
                    ChangeType = ChangeType.Added,
                    ElementType = ApiElementType.Type,
                    ElementName = "NewClass",
                    Description = "Added new class",
                    Severity = SeverityLevel.Info
                }
            }
        };

        // Act
        var report = _formatter.Format(result);

        // Assert
        Assert.NotNull(report);
        Assert.Contains("Added Items", report);
        Assert.Contains("NewClass", report);
        Assert.Contains("Added new class", report);
        Assert.Contains("Type (1)", report); // Group header should show count
    }

    [Fact]
    public void Format_WithRemovedItems_IncludesRemovedSection()
    {
        // Arrange
        var result = new ComparisonResult
        {
            OldAssemblyPath = "source.dll",
            NewAssemblyPath = "target.dll",
            ComparisonTimestamp = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            Configuration = CreateDefaultConfiguration(),
            Differences = new List<ApiDifference>
            {
                new ApiDifference
                {
                    ChangeType = ChangeType.Removed,
                    ElementType = ApiElementType.Method,
                    ElementName = "OldMethod", 
                    Description = "Removed method",
                    Severity = SeverityLevel.Info
                }
            }
        };

        // Act
        var report = _formatter.Format(result);

        // Assert
        Assert.NotNull(report);
        Assert.Contains("Removed Items", report);
        Assert.Contains("OldMethod", report);
        Assert.Contains("Removed method", report);
        Assert.Contains("Method (1)", report); // Group header should show count
    }

    [Fact]
    public void Format_WithBreakingChanges_IncludesBreakingSection()
    {
        // Arrange
        var result = new ComparisonResult
        {
            OldAssemblyPath = "source.dll",
            NewAssemblyPath = "target.dll",
            ComparisonTimestamp = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            Configuration = CreateDefaultConfiguration(),
            Differences = new List<ApiDifference>
            {
                new ApiDifference
                {
                    ChangeType = ChangeType.Removed,
                    ElementType = ApiElementType.Property,
                    ElementName = "ImportantProperty",
                    Description = "Removed important property",
                    IsBreakingChange = true,
                    Severity = SeverityLevel.Critical
                }
            }
        };

        // Act
        var report = _formatter.Format(result);

        // Assert
        Assert.NotNull(report);
        Assert.Contains("Breaking Changes", report);
        Assert.Contains("ImportantProperty", report);
        Assert.Contains("Removed important property", report);
        Assert.Contains("BREAKING", report); // Breaking badge should be present
    }

    [Fact]
    public void Format_WithSignatures_IncludesSignatureDetails()
    {
        // Arrange
        var result = new ComparisonResult
        {
            OldAssemblyPath = "source.dll", 
            NewAssemblyPath = "target.dll",
            ComparisonTimestamp = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            Configuration = CreateDefaultConfiguration(),
            Differences = new List<ApiDifference>
            {
                new ApiDifference
                {
                    ChangeType = ChangeType.Modified,
                    ElementType = ApiElementType.Method,
                    ElementName = "ChangedMethod",
                    Description = "Method signature changed",
                    OldSignature = "public void ChangedMethod(int param)",
                    NewSignature = "public void ChangedMethod(string param)",
                    Severity = SeverityLevel.Warning
                }
            }
        };

        // Act
        var report = _formatter.Format(result);

        // Assert
        Assert.NotNull(report);
        Assert.Contains("ChangedMethod", report);
        Assert.Contains("Method signature changed", report);
        Assert.Contains("Old Signature:", report);
        Assert.Contains("public void ChangedMethod(int param)", report);
        Assert.Contains("New Signature:", report);
        Assert.Contains("public void ChangedMethod(string param)", report);
    }

    private static ComparisonConfiguration CreateDefaultConfiguration()
    {
        return new ComparisonConfiguration
        {
            Filters = new FilterConfiguration(),
            Mappings = new MappingConfiguration(),
            Exclusions = new ExclusionConfiguration(),
            BreakingChangeRules = new BreakingChangeRules(),
            OutputFormat = ReportFormat.Html,
            OutputPath = "test.html"
        };
    }
}

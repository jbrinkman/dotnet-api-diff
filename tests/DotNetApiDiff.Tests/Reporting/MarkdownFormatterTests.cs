// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using DotNetApiDiff.Models;
using DotNetApiDiff.Reporting;
using System.Text.RegularExpressions;
using Xunit;

namespace DotNetApiDiff.Tests.Reporting;

public class MarkdownFormatterTests
{
    [Fact]
    public void Format_EmptyResult_ReturnsBasicMarkdown()
    {
        // Arrange
        var formatter = new MarkdownFormatter();
        var result = new ComparisonResult
        {
            OldAssemblyPath = "OldAssembly.dll",
            NewAssemblyPath = "NewAssembly.dll",
            ComparisonTimestamp = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        };

        // Act
        var markdown = formatter.Format(result);

        // Assert
        Assert.Contains("# API Comparison Report", markdown);
        Assert.Contains("## Metadata", markdown);
        Assert.Contains("| Source Assembly | `OldAssembly.dll` |", markdown);
        Assert.Contains("| Target Assembly | `NewAssembly.dll` |", markdown);
        Assert.Contains("| Comparison Date | 2023-01-01 12:00:00 |", markdown);
        Assert.Contains("| Total Differences | 0 |", markdown);
        Assert.Contains("## Summary", markdown);
        Assert.Contains("| Added | 0 |", markdown);
        Assert.Contains("| Removed | 0 |", markdown);
        Assert.Contains("| Modified | 0 |", markdown);
        Assert.Contains("| Breaking Changes | 0 |", markdown);
        Assert.Contains("| **Total Changes** | **0** |", markdown);

        // Verify breaking changes section is not present
        Assert.DoesNotContain("## Breaking Changes", markdown);
    }

    [Fact]
    public void Format_WithBreakingChanges_IncludesBreakingChangesSection()
    {
        // Arrange
        var formatter = new MarkdownFormatter();
        var result = new ComparisonResult
        {
            OldAssemblyPath = "OldAssembly.dll",
            NewAssemblyPath = "NewAssembly.dll",
            ComparisonTimestamp = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            Differences = new List<ApiDifference>
            {
                new ApiDifference
                {
                    ChangeType = ChangeType.Removed,
                    ElementType = ApiElementType.Method,
                    ElementName = "MyNamespace.MyClass.MyMethod()",
                    Description = "Method was removed",
                    IsBreakingChange = true,
                    Severity = SeverityLevel.Error,
                    OldSignature = "public void MyMethod()"
                }
            },
            Summary = new ComparisonSummary
            {
                RemovedCount = 1,
                BreakingChangesCount = 1
            }
        };

        // Act
        var markdown = formatter.Format(result);

        // Assert
        Assert.Contains("## Breaking Changes", markdown);
        Assert.Contains("| Type | Element | Description | Severity |", markdown);
        Assert.Contains("| Method | `MyNamespace.MyClass.MyMethod()` | Method was removed | Error |", markdown);
        Assert.Contains("## Removed Items (1)", markdown);
        Assert.Contains("### Method", markdown);
        Assert.Contains("| `MyNamespace.MyClass.MyMethod()` | Method was removed | Yes |", markdown);
        Assert.Contains("<details>", markdown);
        Assert.Contains("<summary>Signature Details</summary>", markdown);
        Assert.Contains("**Old:**", markdown);
        Assert.Contains("```csharp", markdown);
        Assert.Contains("public void MyMethod()", markdown);
    }

    [Fact]
    public void Format_WithMultipleChangeTypes_IncludesAllSections()
    {
        // Arrange
        var formatter = new MarkdownFormatter();
        var result = new ComparisonResult
        {
            OldAssemblyPath = "OldAssembly.dll",
            NewAssemblyPath = "NewAssembly.dll",
            ComparisonTimestamp = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            Differences = new List<ApiDifference>
            {
                new ApiDifference
                {
                    ChangeType = ChangeType.Added,
                    ElementType = ApiElementType.Method,
                    ElementName = "MyNamespace.MyClass.NewMethod()",
                    Description = "Method was added",
                    IsBreakingChange = false,
                    NewSignature = "public void NewMethod()"
                },
                new ApiDifference
                {
                    ChangeType = ChangeType.Removed,
                    ElementType = ApiElementType.Method,
                    ElementName = "MyNamespace.MyClass.OldMethod()",
                    Description = "Method was removed",
                    IsBreakingChange = true,
                    Severity = SeverityLevel.Error,
                    OldSignature = "public void OldMethod()"
                },
                new ApiDifference
                {
                    ChangeType = ChangeType.Modified,
                    ElementType = ApiElementType.Method,
                    ElementName = "MyNamespace.MyClass.ChangedMethod()",
                    Description = "Method signature changed",
                    IsBreakingChange = true,
                    Severity = SeverityLevel.Warning,
                    OldSignature = "public void ChangedMethod(int value)",
                    NewSignature = "public void ChangedMethod(string value)"
                },
                new ApiDifference
                {
                    ChangeType = ChangeType.Excluded,
                    ElementType = ApiElementType.Property,
                    ElementName = "MyNamespace.MyClass.ExcludedProperty",
                    Description = "Property was intentionally excluded",
                    IsBreakingChange = false
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
        var markdown = formatter.Format(result);

        // Assert
        // Check for all section headers
        Assert.Contains("## Breaking Changes", markdown);
        Assert.Contains("## Added Items (1)", markdown);
        Assert.Contains("## Removed Items (1)", markdown);
        Assert.Contains("## Modified Items (1)", markdown);
        Assert.Contains("## Excluded/Unsupported Items (1)", markdown);

        // Check for specific content in each section
        Assert.Contains("### Method", markdown);
        Assert.Contains("`MyNamespace.MyClass.NewMethod()`", markdown);
        Assert.Contains("`MyNamespace.MyClass.OldMethod()`", markdown);
        Assert.Contains("`MyNamespace.MyClass.ChangedMethod()`", markdown);
        Assert.Contains("### Property", markdown);
        Assert.Contains("`MyNamespace.MyClass.ExcludedProperty`", markdown);

        // Check for signature details
        Assert.Contains("public void NewMethod()", markdown);
        Assert.Contains("public void OldMethod()", markdown);
        Assert.Contains("public void ChangedMethod(int value)", markdown);
        Assert.Contains("public void ChangedMethod(string value)", markdown);
    }

    [Fact]
    public void Format_WithExcludedItems_IncludesExcludedSection()
    {
        // Arrange
        var formatter = new MarkdownFormatter();
        var result = new ComparisonResult
        {
            OldAssemblyPath = "OldAssembly.dll",
            NewAssemblyPath = "NewAssembly.dll",
            ComparisonTimestamp = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            Differences = new List<ApiDifference>
            {
                new ApiDifference
                {
                    ChangeType = ChangeType.Excluded,
                    ElementType = ApiElementType.Type,
                    ElementName = "MyNamespace.ExcludedType",
                    Description = "Type was intentionally excluded",
                    IsBreakingChange = false
                },
                new ApiDifference
                {
                    ChangeType = ChangeType.Excluded,
                    ElementType = ApiElementType.Method,
                    ElementName = "MyNamespace.MyClass.ExcludedMethod()",
                    Description = "Method was intentionally excluded",
                    IsBreakingChange = false
                }
            },
            Summary = new ComparisonSummary()
        };

        // Act
        var markdown = formatter.Format(result);

        // Assert
        Assert.Contains("## Excluded/Unsupported Items (2)", markdown);
        Assert.Contains("The following items were intentionally excluded from the comparison:", markdown);
        Assert.Contains("### Type", markdown);
        Assert.Contains("`MyNamespace.ExcludedType`", markdown);
        Assert.Contains("### Method", markdown);
        Assert.Contains("`MyNamespace.MyClass.ExcludedMethod()`", markdown);
    }

    [Fact]
    public void Format_OutputIsValidMarkdown()
    {
        // Arrange
        var formatter = new MarkdownFormatter();
        var result = new ComparisonResult
        {
            OldAssemblyPath = "OldAssembly.dll",
            NewAssemblyPath = "NewAssembly.dll",
            ComparisonTimestamp = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            Differences = new List<ApiDifference>
            {
                new ApiDifference
                {
                    ChangeType = ChangeType.Added,
                    ElementType = ApiElementType.Method,
                    ElementName = "MyNamespace.MyClass.NewMethod()",
                    Description = "Method was added",
                    IsBreakingChange = false,
                    NewSignature = "public void NewMethod()"
                }
            },
            Summary = new ComparisonSummary
            {
                AddedCount = 1
            }
        };

        // Act
        var markdown = formatter.Format(result);

        // Assert
        // Normalize line endings for cross-platform compatibility
        var normalizedMarkdown = markdown.Replace("\r\n", "\n").Replace("\r", "\n");

        // Check for valid markdown structure
        // Headers should start with #
        Assert.Matches(new Regex(@"^# .*$", RegexOptions.Multiline), normalizedMarkdown);
        Assert.Matches(new Regex(@"^## .*$", RegexOptions.Multiline), normalizedMarkdown);

        // Tables should have header row and separator row
        Assert.Matches(new Regex(@"^\| .* \|$", RegexOptions.Multiline), normalizedMarkdown);
        Assert.Matches(new Regex(@"^\|\-+\|\-+\|", RegexOptions.Multiline), normalizedMarkdown);

        // Code blocks should be properly formatted
        Assert.Matches(new Regex(@"```csharp\s.*\s```", RegexOptions.Singleline), normalizedMarkdown);

        // Details tags should be properly closed
        var detailsOpenCount = Regex.Matches(normalizedMarkdown, @"<details>").Count;
        var detailsCloseCount = Regex.Matches(normalizedMarkdown, @"</details>").Count;
        Assert.Equal(detailsOpenCount, detailsCloseCount);
    }
}

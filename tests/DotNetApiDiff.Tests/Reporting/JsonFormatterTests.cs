// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using DotNetApiDiff.Models;
using DotNetApiDiff.Reporting;
using System.Text.Json;
using Xunit;

namespace DotNetApiDiff.Tests.Reporting;

public class JsonFormatterTests
{
    private readonly JsonFormatter _formatter;

    public JsonFormatterTests()
    {
        _formatter = new JsonFormatter();
    }

    [Fact]
    public void Format_WithEmptyResult_ReturnsValidJson()
    {
        // Arrange
        var result = new ComparisonResult
        {
            OldAssemblyPath = "source.dll",
            NewAssemblyPath = "target.dll",
            ComparisonTimestamp = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        };

        // Act
        var jsonOutput = _formatter.Format(result);

        // Assert
        Assert.NotNull(jsonOutput);
        Assert.NotEmpty(jsonOutput);
        
        // Verify it's valid JSON
        var exception = Record.Exception(() => JsonDocument.Parse(jsonOutput));
        Assert.Null(exception);
        
        // Verify basic content
        Assert.Contains("source.dll", jsonOutput);
        Assert.Contains("target.dll", jsonOutput);
        Assert.Contains("2023-01-01T12:00:00", jsonOutput);
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
        var jsonOutput = _formatter.Format(result);
        var jsonDoc = JsonDocument.Parse(jsonOutput);

        // Assert
        var root = jsonDoc.RootElement;
        
        // Verify summary
        Assert.Equal(1, root.GetProperty("summary").GetProperty("addedCount").GetInt32());
        
        // Verify added items
        var added = root.GetProperty("added");
        Assert.Equal(1, added.GetArrayLength());
        
        var addedItem = added[0];
        Assert.Equal("Added", addedItem.GetProperty("changeType").GetString());
        Assert.Equal("Method", addedItem.GetProperty("elementType").GetString());
        Assert.Equal("System.String.NewMethod()", addedItem.GetProperty("elementName").GetString());
        Assert.Equal("Method added", addedItem.GetProperty("description").GetString());
        Assert.False(addedItem.GetProperty("isBreakingChange").GetBoolean());
        Assert.Equal("Info", addedItem.GetProperty("severity").GetString());
        Assert.Equal("public void NewMethod()", addedItem.GetProperty("newSignature").GetString());
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
        var jsonOutput = _formatter.Format(result);
        var jsonDoc = JsonDocument.Parse(jsonOutput);

        // Assert
        var root = jsonDoc.RootElement;
        
        // Verify summary
        Assert.Equal(1, root.GetProperty("summary").GetProperty("removedCount").GetInt32());
        Assert.Equal(1, root.GetProperty("summary").GetProperty("breakingChangesCount").GetInt32());
        
        // Verify removed items
        var removed = root.GetProperty("removed");
        Assert.Equal(1, removed.GetArrayLength());
        
        var removedItem = removed[0];
        Assert.Equal("Removed", removedItem.GetProperty("changeType").GetString());
        Assert.Equal("Method", removedItem.GetProperty("elementType").GetString());
        Assert.Equal("System.String.OldMethod()", removedItem.GetProperty("elementName").GetString());
        Assert.Equal("Method removed", removedItem.GetProperty("description").GetString());
        Assert.True(removedItem.GetProperty("isBreakingChange").GetBoolean());
        Assert.Equal("Error", removedItem.GetProperty("severity").GetString());
        Assert.Equal("public void OldMethod()", removedItem.GetProperty("oldSignature").GetString());
        
        // Verify breaking changes section
        var breakingChanges = root.GetProperty("breakingChanges");
        Assert.Equal(1, breakingChanges.GetArrayLength());
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
        var jsonOutput = _formatter.Format(result);
        var jsonDoc = JsonDocument.Parse(jsonOutput);

        // Assert
        var root = jsonDoc.RootElement;
        
        // Verify summary
        Assert.Equal(1, root.GetProperty("summary").GetProperty("modifiedCount").GetInt32());
        Assert.Equal(1, root.GetProperty("summary").GetProperty("breakingChangesCount").GetInt32());
        
        // Verify modified items
        var modified = root.GetProperty("modified");
        Assert.Equal(1, modified.GetArrayLength());
        
        var modifiedItem = modified[0];
        Assert.Equal("Modified", modifiedItem.GetProperty("changeType").GetString());
        Assert.Equal("Property", modifiedItem.GetProperty("elementType").GetString());
        Assert.Equal("System.String.Length", modifiedItem.GetProperty("elementName").GetString());
        Assert.Equal("Property type changed", modifiedItem.GetProperty("description").GetString());
        Assert.True(modifiedItem.GetProperty("isBreakingChange").GetBoolean());
        Assert.Equal("Critical", modifiedItem.GetProperty("severity").GetString());
        Assert.Equal("public int Length { get; }", modifiedItem.GetProperty("oldSignature").GetString());
        Assert.Equal("public long Length { get; }", modifiedItem.GetProperty("newSignature").GetString());
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
        var jsonOutput = _formatter.Format(result);
        var jsonDoc = JsonDocument.Parse(jsonOutput);

        // Assert
        var root = jsonDoc.RootElement;
        
        // Verify summary
        Assert.Equal(1, root.GetProperty("summary").GetProperty("addedCount").GetInt32());
        Assert.Equal(1, root.GetProperty("summary").GetProperty("removedCount").GetInt32());
        Assert.Equal(1, root.GetProperty("summary").GetProperty("modifiedCount").GetInt32());
        Assert.Equal(2, root.GetProperty("summary").GetProperty("breakingChangesCount").GetInt32());
        Assert.Equal(3, root.GetProperty("summary").GetProperty("totalChanges").GetInt32());
        
        // Verify sections exist with correct counts
        Assert.Equal(1, root.GetProperty("added").GetArrayLength());
        Assert.Equal(1, root.GetProperty("removed").GetArrayLength());
        Assert.Equal(1, root.GetProperty("modified").GetArrayLength());
        Assert.Equal(1, root.GetProperty("excluded").GetArrayLength());
        Assert.Equal(2, root.GetProperty("breakingChanges").GetArrayLength());
    }
    
    [Fact]
    public void Format_WithNonIndentedOption_ReturnsCompactJson()
    {
        // Arrange
        var compactFormatter = new JsonFormatter(indented: false);
        var result = new ComparisonResult
        {
            OldAssemblyPath = "source.dll",
            NewAssemblyPath = "target.dll"
        };

        // Act
        var indentedOutput = _formatter.Format(result);
        var compactOutput = compactFormatter.Format(result);

        // Assert
        Assert.NotNull(compactOutput);
        Assert.NotEmpty(compactOutput);
        
        // Compact should be shorter than indented
        Assert.True(compactOutput.Length < indentedOutput.Length);
        
        // But should contain the same data
        var indentedDoc = JsonDocument.Parse(indentedOutput);
        var compactDoc = JsonDocument.Parse(compactOutput);
        
        Assert.Equal(
            indentedDoc.RootElement.GetProperty("oldAssemblyPath").GetString(),
            compactDoc.RootElement.GetProperty("oldAssemblyPath").GetString());
        Assert.Equal(
            indentedDoc.RootElement.GetProperty("newAssemblyPath").GetString(),
            compactDoc.RootElement.GetProperty("newAssemblyPath").GetString());
    }
}

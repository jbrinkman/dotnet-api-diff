// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using DotNetApiDiff.Models;
using Xunit;

namespace DotNetApiDiff.Tests.Models;

public class ApiChangeTests
{
    [Fact]
    public void ApiChange_DefaultConstructor_InitializesCollections()
    {
        // Arrange & Act
        var change = new ApiChange();

        // Assert
        Assert.NotNull(change.Details);
        Assert.Empty(change.Details);
        Assert.Equal(string.Empty, change.Description);
        Assert.False(change.IsBreakingChange);
    }

    [Fact]
    public void IsValid_WithValidAddedChange_ReturnsTrue()
    {
        // Arrange
        var change = new ApiChange
        {
            Type = ChangeType.Added,
            Description = "Added new method",
            TargetMember = new ApiMember { Name = "TestMethod", FullName = "Test.TestMethod", Signature = "void TestMethod()" }
        };

        // Act & Assert
        Assert.True(change.IsValid());
    }

    [Fact]
    public void IsValid_WithValidRemovedChange_ReturnsTrue()
    {
        // Arrange
        var change = new ApiChange
        {
            Type = ChangeType.Removed,
            Description = "Removed method",
            SourceMember = new ApiMember { Name = "TestMethod", FullName = "Test.TestMethod", Signature = "void TestMethod()" }
        };

        // Act & Assert
        Assert.True(change.IsValid());
    }

    [Fact]
    public void IsValid_WithValidModifiedChange_ReturnsTrue()
    {
        // Arrange
        var change = new ApiChange
        {
            Type = ChangeType.Modified,
            Description = "Modified method signature",
            SourceMember = new ApiMember { Name = "TestMethod", FullName = "Test.TestMethod", Signature = "void TestMethod()" },
            TargetMember = new ApiMember { Name = "TestMethod", FullName = "Test.TestMethod", Signature = "int TestMethod()" }
        };

        // Act & Assert
        Assert.True(change.IsValid());
    }

    [Fact]
    public void IsValid_WithEmptyDescription_ReturnsFalse()
    {
        // Arrange
        var change = new ApiChange
        {
            Type = ChangeType.Added,
            Description = string.Empty,
            TargetMember = new ApiMember { Name = "TestMethod", FullName = "Test.TestMethod", Signature = "void TestMethod()" }
        };

        // Act & Assert
        Assert.False(change.IsValid());
    }

    [Fact]
    public void IsValid_AddedChangeWithoutTargetMember_ReturnsFalse()
    {
        // Arrange
        var change = new ApiChange
        {
            Type = ChangeType.Added,
            Description = "Added new method",
            TargetMember = null
        };

        // Act & Assert
        Assert.False(change.IsValid());
    }

    [Fact]
    public void IsValid_RemovedChangeWithoutSourceMember_ReturnsFalse()
    {
        // Arrange
        var change = new ApiChange
        {
            Type = ChangeType.Removed,
            Description = "Removed method",
            SourceMember = null
        };

        // Act & Assert
        Assert.False(change.IsValid());
    }

    [Fact]
    public void IsValid_ModifiedChangeWithoutSourceMember_ReturnsFalse()
    {
        // Arrange
        var change = new ApiChange
        {
            Type = ChangeType.Modified,
            Description = "Modified method",
            SourceMember = null,
            TargetMember = new ApiMember { Name = "TestMethod", FullName = "Test.TestMethod", Signature = "void TestMethod()" }
        };

        // Act & Assert
        Assert.False(change.IsValid());
    }

    [Fact]
    public void IsValid_ModifiedChangeWithoutTargetMember_ReturnsFalse()
    {
        // Arrange
        var change = new ApiChange
        {
            Type = ChangeType.Modified,
            Description = "Modified method",
            SourceMember = new ApiMember { Name = "TestMethod", FullName = "Test.TestMethod", Signature = "void TestMethod()" },
            TargetMember = null
        };

        // Act & Assert
        Assert.False(change.IsValid());
    }

    [Fact]
    public void GetMemberName_WithTargetMember_ReturnsTargetMemberFullName()
    {
        // Arrange
        var change = new ApiChange
        {
            TargetMember = new ApiMember { FullName = "Target.Method" },
            SourceMember = new ApiMember { FullName = "Source.Method" }
        };

        // Act
        var result = change.GetMemberName();

        // Assert
        Assert.Equal("Target.Method", result);
    }

    [Fact]
    public void GetMemberName_WithOnlySourceMember_ReturnsSourceMemberFullName()
    {
        // Arrange
        var change = new ApiChange
        {
            SourceMember = new ApiMember { FullName = "Source.Method" },
            TargetMember = null
        };

        // Act
        var result = change.GetMemberName();

        // Assert
        Assert.Equal("Source.Method", result);
    }

    [Fact]
    public void GetMemberName_WithNoMembers_ReturnsUnknown()
    {
        // Arrange
        var change = new ApiChange
        {
            SourceMember = null,
            TargetMember = null
        };

        // Act
        var result = change.GetMemberName();

        // Assert
        Assert.Equal("Unknown", result);
    }

    [Fact]
    public void ToString_WithBreakingChange_IncludesBreakingIndicator()
    {
        // Arrange
        var change = new ApiChange
        {
            Type = ChangeType.Removed,
            IsBreakingChange = true,
            TargetMember = new ApiMember { FullName = "Test.Method" }
        };

        // Act
        var result = change.ToString();

        // Assert
        Assert.Equal("Removed: Test.Method [BREAKING]", result);
    }

    [Fact]
    public void ToString_WithoutBreakingChange_DoesNotIncludeBreakingIndicator()
    {
        // Arrange
        var change = new ApiChange
        {
            Type = ChangeType.Added,
            IsBreakingChange = false,
            TargetMember = new ApiMember { FullName = "Test.Method" }
        };

        // Act
        var result = change.ToString();

        // Assert
        Assert.Equal("Added: Test.Method", result);
    }

    [Fact]
    public void Details_CanBeModified()
    {
        // Arrange
        var change = new ApiChange();

        // Act
        change.Details.Add("Parameter type changed from int to string");
        change.Details.Add("Return type changed from void to bool");

        // Assert
        Assert.Equal(2, change.Details.Count);
        Assert.Contains("Parameter type changed from int to string", change.Details);
        Assert.Contains("Return type changed from void to bool", change.Details);
    }
}

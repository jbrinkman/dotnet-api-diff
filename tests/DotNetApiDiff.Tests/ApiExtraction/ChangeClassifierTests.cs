// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using DotNetApiDiff.ApiExtraction;
using DotNetApiDiff.Models;
using DotNetApiDiff.Models.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotNetApiDiff.Tests.ApiExtraction;

public class ChangeClassifierTests
{
    private readonly Mock<ILogger<ChangeClassifier>> _loggerMock;
    private readonly BreakingChangeRules _defaultRules;
    private readonly ExclusionConfiguration _defaultExclusions;

    public ChangeClassifierTests()
    {
        _loggerMock = new Mock<ILogger<ChangeClassifier>>();
        _defaultRules = BreakingChangeRules.CreateDefault();
        _defaultExclusions = ExclusionConfiguration.CreateDefault();
    }

    [Fact]
    public void ClassifyChange_NullDifference_ThrowsArgumentNullException()
    {
        // Arrange
        var classifier = new ChangeClassifier(_defaultRules, _defaultExclusions, _loggerMock.Object);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => classifier.ClassifyChange(null!));
    }

    [Fact]
    public void ClassifyChange_AddedType_NotBreakingByDefault()
    {
        // Arrange
        var classifier = new ChangeClassifier(_defaultRules, _defaultExclusions, _loggerMock.Object);
        var difference = new ApiDifference
        {
            ChangeType = ChangeType.Added,
            ElementType = ApiElementType.Type,
            ElementName = "TestNamespace.TestType"
        };

        // Act
        var result = classifier.ClassifyChange(difference);

        // Assert
        Assert.False(result.IsBreakingChange);
        Assert.Equal(SeverityLevel.Info, result.Severity);
    }

    [Fact]
    public void ClassifyChange_AddedType_BreakingWhenConfigured()
    {
        // Arrange
        var rules = new BreakingChangeRules
        {
            TreatAddedTypeAsBreaking = true
        };
        var classifier = new ChangeClassifier(rules, _defaultExclusions, _loggerMock.Object);
        var difference = new ApiDifference
        {
            ChangeType = ChangeType.Added,
            ElementType = ApiElementType.Type,
            ElementName = "TestNamespace.TestType"
        };

        // Act
        var result = classifier.ClassifyChange(difference);

        // Assert
        Assert.True(result.IsBreakingChange);
        Assert.Equal(SeverityLevel.Warning, result.Severity);
    }

    [Fact]
    public void ClassifyChange_RemovedType_BreakingByDefault()
    {
        // Arrange
        var classifier = new ChangeClassifier(_defaultRules, _defaultExclusions, _loggerMock.Object);
        var difference = new ApiDifference
        {
            ChangeType = ChangeType.Removed,
            ElementType = ApiElementType.Type,
            ElementName = "TestNamespace.TestType"
        };

        // Act
        var result = classifier.ClassifyChange(difference);

        // Assert
        Assert.True(result.IsBreakingChange);
        Assert.Equal(SeverityLevel.Error, result.Severity);
    }

    [Fact]
    public void ClassifyChange_RemovedType_NotBreakingWhenConfigured()
    {
        // Arrange
        var rules = new BreakingChangeRules
        {
            TreatTypeRemovalAsBreaking = false
        };
        var classifier = new ChangeClassifier(rules, _defaultExclusions, _loggerMock.Object);
        var difference = new ApiDifference
        {
            ChangeType = ChangeType.Removed,
            ElementType = ApiElementType.Type,
            ElementName = "TestNamespace.TestType"
        };

        // Act
        var result = classifier.ClassifyChange(difference);

        // Assert
        Assert.False(result.IsBreakingChange);
        Assert.Equal(SeverityLevel.Warning, result.Severity);
    }

    [Fact]
    public void ClassifyChange_ModifiedType_BreakingWhenSignatureChanges()
    {
        // Arrange
        var classifier = new ChangeClassifier(_defaultRules, _defaultExclusions, _loggerMock.Object);
        var difference = new ApiDifference
        {
            ChangeType = ChangeType.Modified,
            ElementType = ApiElementType.Type,
            ElementName = "TestNamespace.TestType",
            OldSignature = "public class TestType",
            NewSignature = "public sealed class TestType"
        };

        // Act
        var result = classifier.ClassifyChange(difference);

        // Assert
        Assert.True(result.IsBreakingChange);
        Assert.Equal(SeverityLevel.Error, result.Severity);
    }

    [Fact]
    public void ClassifyChange_ModifiedType_NotBreakingWhenSignatureChangesButConfiguredAsNonBreaking()
    {
        // Arrange
        var rules = new BreakingChangeRules
        {
            TreatSignatureChangeAsBreaking = false
        };
        var classifier = new ChangeClassifier(rules, _defaultExclusions, _loggerMock.Object);
        var difference = new ApiDifference
        {
            ChangeType = ChangeType.Modified,
            ElementType = ApiElementType.Type,
            ElementName = "TestNamespace.TestType",
            OldSignature = "public class TestType",
            NewSignature = "public sealed class TestType",
            IsBreakingChange = false // Set by DifferenceCalculator
        };

        // Act
        var result = classifier.ClassifyChange(difference);

        // Assert
        Assert.False(result.IsBreakingChange);
        Assert.Equal(SeverityLevel.Info, result.Severity);
    }

    [Fact]
    public void ClassifyChange_ExcludedType_MarkedAsExcluded()
    {
        // Arrange
        var exclusions = new ExclusionConfiguration
        {
            ExcludedTypes = new List<string> { "TestNamespace.TestType" }
        };
        var classifier = new ChangeClassifier(_defaultRules, exclusions, _loggerMock.Object);
        var difference = new ApiDifference
        {
            ChangeType = ChangeType.Removed,
            ElementType = ApiElementType.Type,
            ElementName = "TestNamespace.TestType",
            IsBreakingChange = true // Initially set as breaking
        };

        // Act
        var result = classifier.ClassifyChange(difference);

        // Assert
        Assert.Equal(ChangeType.Excluded, result.ChangeType);
        Assert.False(result.IsBreakingChange);
        Assert.Equal(SeverityLevel.Info, result.Severity);
    }

    [Fact]
    public void ClassifyChange_ExcludedMember_MarkedAsExcluded()
    {
        // Arrange
        var exclusions = new ExclusionConfiguration
        {
            ExcludedMembers = new List<string> { "TestNamespace.TestType.TestMethod" }
        };
        var classifier = new ChangeClassifier(_defaultRules, exclusions, _loggerMock.Object);
        var difference = new ApiDifference
        {
            ChangeType = ChangeType.Removed,
            ElementType = ApiElementType.Method,
            ElementName = "TestNamespace.TestType.TestMethod",
            IsBreakingChange = true // Initially set as breaking
        };

        // Act
        var result = classifier.ClassifyChange(difference);

        // Assert
        Assert.Equal(ChangeType.Excluded, result.ChangeType);
        Assert.False(result.IsBreakingChange);
        Assert.Equal(SeverityLevel.Info, result.Severity);
    }

    [Fact]
    public void IsTypeExcluded_ExactMatch_ReturnsTrue()
    {
        // Arrange
        var exclusions = new ExclusionConfiguration
        {
            ExcludedTypes = new List<string> { "TestNamespace.TestType" }
        };
        var classifier = new ChangeClassifier(_defaultRules, exclusions, _loggerMock.Object);

        // Act
        var result = classifier.IsTypeExcluded("TestNamespace.TestType");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsTypeExcluded_NoMatch_ReturnsFalse()
    {
        // Arrange
        var exclusions = new ExclusionConfiguration
        {
            ExcludedTypes = new List<string> { "TestNamespace.OtherType" }
        };
        var classifier = new ChangeClassifier(_defaultRules, exclusions, _loggerMock.Object);

        // Act
        var result = classifier.IsTypeExcluded("TestNamespace.TestType");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsTypeExcluded_PatternMatch_ReturnsTrue()
    {
        // Arrange
        var exclusions = new ExclusionConfiguration
        {
            ExcludedTypePatterns = new List<string> { "TestNamespace.*" }
        };
        var classifier = new ChangeClassifier(_defaultRules, exclusions, _loggerMock.Object);

        // Act
        var result = classifier.IsTypeExcluded("TestNamespace.TestType");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMemberExcluded_ExactMatch_ReturnsTrue()
    {
        // Arrange
        var exclusions = new ExclusionConfiguration
        {
            ExcludedMembers = new List<string> { "TestNamespace.TestType.TestMethod" }
        };
        var classifier = new ChangeClassifier(_defaultRules, exclusions, _loggerMock.Object);

        // Act
        var result = classifier.IsMemberExcluded("TestNamespace.TestType.TestMethod");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMemberExcluded_NoMatch_ReturnsFalse()
    {
        // Arrange
        var exclusions = new ExclusionConfiguration
        {
            ExcludedMembers = new List<string> { "TestNamespace.TestType.OtherMethod" }
        };
        var classifier = new ChangeClassifier(_defaultRules, exclusions, _loggerMock.Object);

        // Act
        var result = classifier.IsMemberExcluded("TestNamespace.TestType.TestMethod");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsMemberExcluded_PatternMatch_ReturnsTrue()
    {
        // Arrange
        var exclusions = new ExclusionConfiguration
        {
            ExcludedMemberPatterns = new List<string> { "TestNamespace.TestType.*" }
        };
        var classifier = new ChangeClassifier(_defaultRules, exclusions, _loggerMock.Object);

        // Act
        var result = classifier.IsMemberExcluded("TestNamespace.TestType.TestMethod");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMemberExcluded_DeclaringTypeExcluded_ReturnsTrue()
    {
        // Arrange
        var exclusions = new ExclusionConfiguration
        {
            ExcludedTypes = new List<string> { "TestNamespace.TestType" }
        };
        var classifier = new ChangeClassifier(_defaultRules, exclusions, _loggerMock.Object);

        // Act
        var result = classifier.IsMemberExcluded("TestNamespace.TestType.TestMethod");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMemberExcluded_DeclaringTypePatternExcluded_ReturnsTrue()
    {
        // Arrange
        var exclusions = new ExclusionConfiguration
        {
            ExcludedTypePatterns = new List<string> { "TestNamespace.*" }
        };
        var classifier = new ChangeClassifier(_defaultRules, exclusions, _loggerMock.Object);

        // Act
        var result = classifier.IsMemberExcluded("TestNamespace.TestType.TestMethod");

        // Assert
        Assert.True(result);
    }
}

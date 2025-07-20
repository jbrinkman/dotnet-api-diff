// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using DotNetApiDiff.ApiExtraction;
using DotNetApiDiff.Interfaces;
using DotNetApiDiff.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotNetApiDiff.Tests.ApiExtraction;

public class DifferenceCalculatorTests
{
    private readonly Mock<ITypeAnalyzer> _mockTypeAnalyzer;
    private readonly Mock<ILogger<DifferenceCalculator>> _mockLogger;
    private readonly DifferenceCalculator _differenceCalculator;

    public DifferenceCalculatorTests()
    {
        _mockTypeAnalyzer = new Mock<ITypeAnalyzer>();
        _mockLogger = new Mock<ILogger<DifferenceCalculator>>();
        _differenceCalculator = new DifferenceCalculator(_mockTypeAnalyzer.Object, _mockLogger.Object);
    }

    [Fact]
    public void CalculateAddedType_NullType_ThrowsArgumentNullException()
    {
        // Arrange
        Type? type = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _differenceCalculator.CalculateAddedType(type!));
    }

    [Fact]
    public void CalculateAddedType_ValidType_ReturnsCorrectDifference()
    {
        // Arrange
        var type = typeof(string);
        var typeMember = new ApiMember
        {
            Name = "String",
            FullName = "System.String",
            Signature = "public sealed class String",
            Type = MemberType.Class,
            Accessibility = AccessibilityLevel.Public
        };

        _mockTypeAnalyzer.Setup(x => x.AnalyzeType(type)).Returns(typeMember);

        // Act
        var result = _differenceCalculator.CalculateAddedType(type);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ChangeType.Added, result.ChangeType);
        Assert.Equal(ApiElementType.Type, result.ElementType);
        Assert.Equal("System.String", result.ElementName);
        Assert.Contains("Added class 'System.String'", result.Description);
        Assert.False(result.IsBreakingChange);
        Assert.Equal(SeverityLevel.Info, result.Severity);
        Assert.Equal(typeMember.Signature, result.NewSignature);
    }

    [Fact]
    public void CalculateRemovedType_NullType_ThrowsArgumentNullException()
    {
        // Arrange
        Type? type = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _differenceCalculator.CalculateRemovedType(type!));
    }

    [Fact]
    public void CalculateRemovedType_ValidType_ReturnsCorrectDifference()
    {
        // Arrange
        var type = typeof(string);
        var typeMember = new ApiMember
        {
            Name = "String",
            FullName = "System.String",
            Signature = "public sealed class String",
            Type = MemberType.Class,
            Accessibility = AccessibilityLevel.Public
        };

        _mockTypeAnalyzer.Setup(x => x.AnalyzeType(type)).Returns(typeMember);

        // Act
        var result = _differenceCalculator.CalculateRemovedType(type);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ChangeType.Removed, result.ChangeType);
        Assert.Equal(ApiElementType.Type, result.ElementType);
        Assert.Equal("System.String", result.ElementName);
        Assert.Contains("Removed class 'System.String'", result.Description);
        Assert.True(result.IsBreakingChange);
        Assert.Equal(SeverityLevel.Error, result.Severity);
        Assert.Equal(typeMember.Signature, result.OldSignature);
    }

    [Fact]
    public void CalculateTypeChanges_NullOldType_ThrowsArgumentNullException()
    {
        // Arrange
        Type? oldType = null;
        Type newType = typeof(string);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _differenceCalculator.CalculateTypeChanges(oldType!, newType));
    }

    [Fact]
    public void CalculateTypeChanges_NullNewType_ThrowsArgumentNullException()
    {
        // Arrange
        Type oldType = typeof(string);
        Type? newType = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _differenceCalculator.CalculateTypeChanges(oldType, newType!));
    }

    [Fact]
    public void CalculateTypeChanges_NoChanges_ReturnsNull()
    {
        // Arrange
        var oldType = typeof(string);
        var newType = typeof(string);

        var typeMember = new ApiMember
        {
            Name = "String",
            FullName = "System.String",
            Signature = "public sealed class String",
            Type = MemberType.Class,
            Accessibility = AccessibilityLevel.Public
        };

        _mockTypeAnalyzer.Setup(x => x.AnalyzeType(oldType)).Returns(typeMember);
        _mockTypeAnalyzer.Setup(x => x.AnalyzeType(newType)).Returns(typeMember);

        // Act
        var result = _differenceCalculator.CalculateTypeChanges(oldType, newType);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void CalculateTypeChanges_AccessibilityChanged_ReturnsCorrectDifference()
    {
        // Arrange
        var oldType = typeof(string);
        var newType = typeof(int);

        var oldTypeMember = new ApiMember
        {
            Name = "String",
            FullName = "System.String",
            Signature = "public sealed class String",
            Type = MemberType.Class,
            Accessibility = AccessibilityLevel.Public
        };

        var newTypeMember = new ApiMember
        {
            Name = "String",
            FullName = "System.String",
            // Make sure signatures are different to trigger the change detection
            Signature = "internal sealed class String",
            Type = MemberType.Class,
            Accessibility = AccessibilityLevel.Internal
        };

        _mockTypeAnalyzer.Setup(x => x.AnalyzeType(oldType)).Returns(oldTypeMember);
        _mockTypeAnalyzer.Setup(x => x.AnalyzeType(newType)).Returns(newTypeMember);

        // Act
        var result = _differenceCalculator.CalculateTypeChanges(oldType, newType);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ChangeType.Modified, result.ChangeType);
        Assert.Equal(ApiElementType.Type, result.ElementType);
        Assert.Equal("System.String", result.ElementName);
        Assert.Contains("Modified class 'System.String'", result.Description);
        Assert.True(result.IsBreakingChange);
        Assert.Equal(SeverityLevel.Error, result.Severity);
        Assert.Equal(oldTypeMember.Signature, result.OldSignature);
        Assert.Equal(newTypeMember.Signature, result.NewSignature);
    }

    [Fact]
    public void CalculateAddedMember_NullMember_ThrowsArgumentNullException()
    {
        // Arrange
        ApiMember? member = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _differenceCalculator.CalculateAddedMember(member!));
    }

    [Fact]
    public void CalculateAddedMember_ValidMember_ReturnsCorrectDifference()
    {
        // Arrange
        var member = new ApiMember
        {
            Name = "ToString",
            FullName = "System.String.ToString",
            Signature = "public override string ToString()",
            Type = MemberType.Method,
            Accessibility = AccessibilityLevel.Public
        };

        // Act
        var result = _differenceCalculator.CalculateAddedMember(member);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ChangeType.Added, result.ChangeType);
        Assert.Equal(ApiElementType.Method, result.ElementType);
        Assert.Equal("System.String.ToString", result.ElementName);
        Assert.Contains("Added method 'System.String.ToString'", result.Description);
        Assert.False(result.IsBreakingChange);
        Assert.Equal(SeverityLevel.Info, result.Severity);
        Assert.Equal(member.Signature, result.NewSignature);
    }

    [Fact]
    public void CalculateRemovedMember_NullMember_ThrowsArgumentNullException()
    {
        // Arrange
        ApiMember? member = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _differenceCalculator.CalculateRemovedMember(member!));
    }

    [Fact]
    public void CalculateRemovedMember_ValidMember_ReturnsCorrectDifference()
    {
        // Arrange
        var member = new ApiMember
        {
            Name = "ToString",
            FullName = "System.String.ToString",
            Signature = "public override string ToString()",
            Type = MemberType.Method,
            Accessibility = AccessibilityLevel.Public
        };

        // Act
        var result = _differenceCalculator.CalculateRemovedMember(member);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ChangeType.Removed, result.ChangeType);
        Assert.Equal(ApiElementType.Method, result.ElementType);
        Assert.Equal("System.String.ToString", result.ElementName);
        Assert.Contains("Removed method 'System.String.ToString'", result.Description);
        Assert.True(result.IsBreakingChange);
        Assert.Equal(SeverityLevel.Error, result.Severity);
        Assert.Equal(member.Signature, result.OldSignature);
    }

    [Fact]
    public void CalculateMemberChanges_NullOldMember_ThrowsArgumentNullException()
    {
        // Arrange
        ApiMember? oldMember = null;
        var newMember = new ApiMember
        {
            Name = "ToString",
            FullName = "System.String.ToString",
            Signature = "public override string ToString()",
            Type = MemberType.Method,
            Accessibility = AccessibilityLevel.Public
        };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _differenceCalculator.CalculateMemberChanges(oldMember!, newMember));
    }

    [Fact]
    public void CalculateMemberChanges_NullNewMember_ThrowsArgumentNullException()
    {
        // Arrange
        var oldMember = new ApiMember
        {
            Name = "ToString",
            FullName = "System.String.ToString",
            Signature = "public override string ToString()",
            Type = MemberType.Method,
            Accessibility = AccessibilityLevel.Public
        };
        ApiMember? newMember = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _differenceCalculator.CalculateMemberChanges(oldMember, newMember!));
    }

    [Fact]
    public void CalculateMemberChanges_NoChanges_ReturnsNull()
    {
        // Arrange
        var oldMember = new ApiMember
        {
            Name = "ToString",
            FullName = "System.String.ToString",
            Signature = "public override string ToString()",
            Type = MemberType.Method,
            Accessibility = AccessibilityLevel.Public
        };

        var newMember = new ApiMember
        {
            Name = "ToString",
            FullName = "System.String.ToString",
            Signature = "public override string ToString()",
            Type = MemberType.Method,
            Accessibility = AccessibilityLevel.Public
        };

        // Act
        var result = _differenceCalculator.CalculateMemberChanges(oldMember, newMember);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void CalculateMemberChanges_AccessibilityChanged_ReturnsCorrectDifference()
    {
        // Arrange
        var oldMember = new ApiMember
        {
            Name = "ToString",
            FullName = "System.String.ToString",
            Signature = "public override string ToString()",
            Type = MemberType.Method,
            Accessibility = AccessibilityLevel.Public
        };

        var newMember = new ApiMember
        {
            Name = "ToString",
            FullName = "System.String.ToString",
            // Make sure signatures are different to trigger the change detection
            Signature = "protected override string ToString()",
            Type = MemberType.Method,
            Accessibility = AccessibilityLevel.Protected
        };

        // Act
        var result = _differenceCalculator.CalculateMemberChanges(oldMember, newMember);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ChangeType.Modified, result.ChangeType);
        Assert.Equal(ApiElementType.Method, result.ElementType);
        Assert.Equal("System.String.ToString", result.ElementName);
        Assert.Contains("Modified method 'System.String.ToString'", result.Description);
        Assert.True(result.IsBreakingChange);
        Assert.Equal(SeverityLevel.Error, result.Severity);
        Assert.Equal(oldMember.Signature, result.OldSignature);
        Assert.Equal(newMember.Signature, result.NewSignature);
    }

    [Fact]
    public void CalculateMemberChanges_AttributesChanged_ReturnsCorrectDifference()
    {
        // Arrange
        var oldMember = new ApiMember
        {
            Name = "ToString",
            FullName = "System.String.ToString",
            Signature = "public override string ToString()",
            Type = MemberType.Method,
            Accessibility = AccessibilityLevel.Public,
            Attributes = new List<string> { "ObsoleteAttribute" }
        };

        var newMember = new ApiMember
        {
            Name = "ToString",
            FullName = "System.String.ToString",
            // Make sure signatures are different to trigger the change detection
            Signature = "public override string ToString() [different]",
            Type = MemberType.Method,
            Accessibility = AccessibilityLevel.Public,
            Attributes = new List<string>()
        };

        // Act
        var result = _differenceCalculator.CalculateMemberChanges(oldMember, newMember);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ChangeType.Modified, result.ChangeType);
        Assert.Equal(ApiElementType.Method, result.ElementType);
        Assert.Equal("System.String.ToString", result.ElementName);
        Assert.Contains("Modified method 'System.String.ToString'", result.Description);
        Assert.Equal(oldMember.Signature, result.OldSignature);
        Assert.Equal(newMember.Signature, result.NewSignature);
    }
}

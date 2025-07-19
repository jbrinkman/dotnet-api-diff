// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using DotNetApiDiff.Models;
using Xunit;

namespace DotNetApiDiff.Tests.Models;

public class ApiMemberTests
{
    [Fact]
    public void ApiMember_DefaultConstructor_InitializesCollections()
    {
        // Arrange & Act
        var member = new ApiMember();

        // Assert
        Assert.NotNull(member.Attributes);
        Assert.Empty(member.Attributes);
        Assert.Equal(string.Empty, member.Name);
        Assert.Equal(string.Empty, member.FullName);
        Assert.Equal(string.Empty, member.Signature);
        Assert.Equal(string.Empty, member.DeclaringType);
        Assert.Equal(string.Empty, member.Namespace);
    }

    [Fact]
    public void IsValid_WithValidData_ReturnsTrue()
    {
        // Arrange
        var member = new ApiMember
        {
            Name = "TestMethod",
            FullName = "TestNamespace.TestClass.TestMethod",
            Signature = "void TestMethod()",
            Type = MemberType.Method,
            Accessibility = AccessibilityLevel.Public
        };

        // Act & Assert
        Assert.True(member.IsValid());
    }

    [Theory]
    [InlineData("", "FullName", "Signature")]
    [InlineData("Name", "", "Signature")]
    [InlineData("Name", "FullName", "")]
    [InlineData(null, "FullName", "Signature")]
    [InlineData("Name", null, "Signature")]
    [InlineData("Name", "FullName", null)]
    public void IsValid_WithInvalidData_ReturnsFalse(string? name, string? fullName, string? signature)
    {
        // Arrange
        var member = new ApiMember
        {
            Name = name ?? string.Empty,
            FullName = fullName ?? string.Empty,
            Signature = signature ?? string.Empty
        };

        // Act & Assert
        Assert.False(member.IsValid());
    }

    [Fact]
    public void ToString_ReturnsExpectedFormat()
    {
        // Arrange
        var member = new ApiMember
        {
            Type = MemberType.Method,
            FullName = "TestNamespace.TestClass.TestMethod",
            Accessibility = AccessibilityLevel.Public
        };

        // Act
        var result = member.ToString();

        // Assert
        Assert.Equal("Method TestNamespace.TestClass.TestMethod (Public)", result);
    }

    [Fact]
    public void Equals_WithSameFullNameAndSignature_ReturnsTrue()
    {
        // Arrange
        var member1 = new ApiMember
        {
            FullName = "TestClass.TestMethod",
            Signature = "void TestMethod()"
        };
        var member2 = new ApiMember
        {
            FullName = "TestClass.TestMethod",
            Signature = "void TestMethod()"
        };

        // Act & Assert
        Assert.True(member1.Equals(member2));
        Assert.Equal(member1.GetHashCode(), member2.GetHashCode());
    }

    [Fact]
    public void Equals_WithDifferentFullName_ReturnsFalse()
    {
        // Arrange
        var member1 = new ApiMember
        {
            FullName = "TestClass.TestMethod1",
            Signature = "void TestMethod()"
        };
        var member2 = new ApiMember
        {
            FullName = "TestClass.TestMethod2",
            Signature = "void TestMethod()"
        };

        // Act & Assert
        Assert.False(member1.Equals(member2));
    }

    [Fact]
    public void Equals_WithDifferentSignature_ReturnsFalse()
    {
        // Arrange
        var member1 = new ApiMember
        {
            FullName = "TestClass.TestMethod",
            Signature = "void TestMethod()"
        };
        var member2 = new ApiMember
        {
            FullName = "TestClass.TestMethod",
            Signature = "int TestMethod()"
        };

        // Act & Assert
        Assert.False(member1.Equals(member2));
    }

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        // Arrange
        var member = new ApiMember();

        // Act & Assert
        Assert.False(member.Equals(null));
    }

    [Fact]
    public void Equals_WithDifferentType_ReturnsFalse()
    {
        // Arrange
        var member = new ApiMember();

        // Act & Assert
        Assert.False(member.Equals("not an ApiMember"));
    }

    [Fact]
    public void Attributes_CanBeModified()
    {
        // Arrange
        var member = new ApiMember();

        // Act
        member.Attributes.Add("System.ObsoleteAttribute");
        member.Attributes.Add("System.ComponentModel.EditorBrowsableAttribute");

        // Assert
        Assert.Equal(2, member.Attributes.Count);
        Assert.Contains("System.ObsoleteAttribute", member.Attributes);
        Assert.Contains("System.ComponentModel.EditorBrowsableAttribute", member.Attributes);
    }
}

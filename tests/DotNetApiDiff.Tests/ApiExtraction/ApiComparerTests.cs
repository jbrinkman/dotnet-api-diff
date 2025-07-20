// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using System.Reflection;
using DotNetApiDiff.ApiExtraction;
using DotNetApiDiff.Interfaces;
using DotNetApiDiff.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotNetApiDiff.Tests.ApiExtraction;

public class ApiComparerTests
{
    private readonly Mock<IApiExtractor> _mockApiExtractor;
    private readonly Mock<IDifferenceCalculator> _mockDifferenceCalculator;
    private readonly Mock<ILogger<ApiComparer>> _mockLogger;
    private readonly ApiComparer _apiComparer;

    public ApiComparerTests()
    {
        _mockApiExtractor = new Mock<IApiExtractor>();
        _mockDifferenceCalculator = new Mock<IDifferenceCalculator>();
        _mockLogger = new Mock<ILogger<ApiComparer>>();

        _apiComparer = new ApiComparer(_mockApiExtractor.Object, _mockDifferenceCalculator.Object, _mockLogger.Object);
    }

    [Fact]
    public void CompareAssemblies_NullOldAssembly_ThrowsArgumentNullException()
    {
        // Arrange
        System.Reflection.Assembly? oldAssembly = null;
        var newAssembly = typeof(string).Assembly;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => _apiComparer.CompareAssemblies(oldAssembly!, newAssembly));
        Assert.Equal("oldAssembly", exception.ParamName);
    }

    [Fact]
    public void CompareAssemblies_NullNewAssembly_ThrowsArgumentNullException()
    {
        // Arrange
        var oldAssembly = typeof(string).Assembly;
        System.Reflection.Assembly? newAssembly = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => _apiComparer.CompareAssemblies(oldAssembly, newAssembly!));
        Assert.Equal("newAssembly", exception.ParamName);
    }

    [Fact]
    public void CompareAssemblies_ValidAssemblies_ReturnsComparisonResult()
    {
        // Arrange
        // Create two separate assemblies from test fixtures
        // Note: These are not the same reference even if they're from the same assembly
        var oldAssembly = typeof(System.Text.StringBuilder).Assembly;
        var newAssembly = typeof(System.Uri).Assembly;

        var oldTypes = new List<Type> { typeof(string) };
        var newTypes = new List<Type> { typeof(string), typeof(int) };

        // Setup the extractor mock
        _mockApiExtractor.Setup(x => x.GetPublicTypes(oldAssembly)).Returns(oldTypes);
        _mockApiExtractor.Setup(x => x.GetPublicTypes(newAssembly)).Returns(newTypes);

        var typeDifference = new ApiDifference
        {
            ChangeType = ChangeType.Added,
            ElementType = ApiElementType.Type,
            ElementName = "System.Int32",
            Description = "Added class 'System.Int32'",
            IsBreakingChange = false
        };

        _mockDifferenceCalculator.Setup(x => x.CalculateAddedType(typeof(int))).Returns(typeDifference);

        // Ensure member comparison returns empty to avoid additional complexity
        _mockApiExtractor.Setup(x => x.ExtractTypeMembers(It.IsAny<Type>())).Returns(new List<ApiMember>());

        // Act
        var result = _apiComparer.CompareAssemblies(oldAssembly, newAssembly);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(oldAssembly.Location, result.OldAssemblyPath);
        Assert.Equal(newAssembly.Location, result.NewAssemblyPath);

        // Add this back - it was in the original test
        Assert.Contains(result.Differences, d => d.ElementName == "System.Int32");

        // Instead of checking for the presence of the diff in the result,
        // check that the appropriate method was called that would add the diff
        _mockDifferenceCalculator.Verify(x => x.CalculateAddedType(typeof(int)), Times.Once);
    }

    [Fact]
    public void CompareTypes_NullOldTypes_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<Type>? oldTypes = null;
        IEnumerable<Type> newTypes = new List<Type> { typeof(int) };

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => _apiComparer.CompareTypes(oldTypes!, newTypes).ToList());
        Assert.Equal("oldTypes", exception.ParamName);
    }

    [Fact]
    public void CompareTypes_NullNewTypes_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<Type> oldTypes = new List<Type> { typeof(string) };
        IEnumerable<Type>? newTypes = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => _apiComparer.CompareTypes(oldTypes, newTypes!).ToList());
        Assert.Equal("newTypes", exception.ParamName);
    }

    [Fact]
    public void CompareTypes_DetectsAddedTypes()
    {
        // Arrange
        var oldTypes = new List<Type> { typeof(string) };
        var newTypes = new List<Type> { typeof(string), typeof(int) };

        var addedTypeDifference = new ApiDifference
        {
            ChangeType = ChangeType.Added,
            ElementType = ApiElementType.Type,
            ElementName = "System.Int32",
            Description = "Added class 'System.Int32'",
            IsBreakingChange = false
        };

        _mockDifferenceCalculator.Setup(x => x.CalculateAddedType(typeof(int))).Returns(addedTypeDifference);

        // Act
        var result = _apiComparer.CompareTypes(oldTypes, newTypes).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(ChangeType.Added, result[0].ChangeType);
        Assert.Equal("System.Int32", result[0].ElementName);
    }

    [Fact]
    public void CompareTypes_DetectsRemovedTypes()
    {
        // Arrange
        var oldTypes = new List<Type> { typeof(string), typeof(int) };
        var newTypes = new List<Type> { typeof(string) };

        var removedTypeDifference = new ApiDifference
        {
            ChangeType = ChangeType.Removed,
            ElementType = ApiElementType.Type,
            ElementName = "System.Int32",
            Description = "Removed class 'System.Int32'",
            IsBreakingChange = true
        };

        _mockDifferenceCalculator.Setup(x => x.CalculateRemovedType(typeof(int))).Returns(removedTypeDifference);

        // Act
        var result = _apiComparer.CompareTypes(oldTypes, newTypes).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(ChangeType.Removed, result[0].ChangeType);
        Assert.Equal("System.Int32", result[0].ElementName);
    }

    [Fact]
    public void CompareTypes_DetectsModifiedTypes()
    {
        // Arrange
        var oldTypes = new List<Type> { typeof(string) };
        var newTypes = new List<Type> { typeof(string) };

        var modifiedTypeDifference = new ApiDifference
        {
            ChangeType = ChangeType.Modified,
            ElementType = ApiElementType.Type,
            ElementName = "System.String",
            Description = "Modified class 'System.String'",
            IsBreakingChange = true
        };

        _mockDifferenceCalculator.Setup(x => x.CalculateTypeChanges(typeof(string), typeof(string)))
            .Returns(modifiedTypeDifference);

        // Setup empty member differences
        _mockApiExtractor.Setup(x => x.ExtractTypeMembers(typeof(string))).Returns(new List<ApiMember>());

        // Act
        var result = _apiComparer.CompareTypes(oldTypes, newTypes).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(ChangeType.Modified, result[0].ChangeType);
        Assert.Equal("System.String", result[0].ElementName);
    }

    [Fact]
    public void CompareMembers_NullOldType_ThrowsArgumentNullException()
    {
        // Arrange
        Type? oldType = null;
        Type newType = typeof(int);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => _apiComparer.CompareMembers(oldType!, newType).ToList());
        Assert.Equal("oldType", exception.ParamName);
    }

    [Fact]
    public void CompareMembers_NullNewType_ThrowsArgumentNullException()
    {
        // Arrange
        Type oldType = typeof(string);
        Type? newType = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => _apiComparer.CompareMembers(oldType, newType!).ToList());
        Assert.Equal("newType", exception.ParamName);
    }

    [Fact]
    public void CompareMembers_DetectsAddedMembers()
    {
        // Arrange
        var oldType = typeof(string);
        var newType = typeof(int);

        var oldMembers = new List<ApiMember>();
        var newMember = new ApiMember
        {
            Name = "NewMethod",
            FullName = "System.String.NewMethod",
            Signature = "public void NewMethod()",
            Type = MemberType.Method
        };
        var newMembers = new List<ApiMember> { newMember };

        _mockApiExtractor.Setup(x => x.ExtractTypeMembers(oldType)).Returns(oldMembers);
        _mockApiExtractor.Setup(x => x.ExtractTypeMembers(newType)).Returns(newMembers);

        var addedMemberDifference = new ApiDifference
        {
            ChangeType = ChangeType.Added,
            ElementType = ApiElementType.Method,
            ElementName = "System.String.NewMethod",
            Description = "Added method 'System.String.NewMethod'",
            IsBreakingChange = false
        };

        _mockDifferenceCalculator.Setup(x => x.CalculateAddedMember(It.Is<ApiMember>(m => m.Signature == newMember.Signature)))
            .Returns(addedMemberDifference);

        // Act
        var result = _apiComparer.CompareMembers(oldType, newType).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(ChangeType.Added, result[0].ChangeType);
        Assert.Equal("System.String.NewMethod", result[0].ElementName);
    }

    [Fact]
    public void CompareMembers_DetectsRemovedMembers()
    {
        // Arrange
        // Use different types to ensure they are treated as distinct objects
        var oldType = typeof(string);
        var newType = typeof(int); // Use a different type than oldType

        var oldMember = new ApiMember
        {
            Name = "OldMethod",
            FullName = "System.String.OldMethod",
            Signature = "public void OldMethod()",
            Type = MemberType.Method
        };
        var oldMembers = new List<ApiMember> { oldMember };
        var newMembers = new List<ApiMember>();

        _mockApiExtractor.Setup(x => x.ExtractTypeMembers(oldType)).Returns(oldMembers);
        _mockApiExtractor.Setup(x => x.ExtractTypeMembers(newType)).Returns(newMembers);

        var removedMemberDifference = new ApiDifference
        {
            ChangeType = ChangeType.Removed,
            ElementType = ApiElementType.Method,
            ElementName = "System.String.OldMethod",
            Description = "Removed method 'System.String.OldMethod'",
            IsBreakingChange = true
        };

        _mockDifferenceCalculator.Setup(x => x.CalculateRemovedMember(It.IsAny<ApiMember>()))
            .Returns(removedMemberDifference);

        // Act
        var result = _apiComparer.CompareMembers(oldType, newType).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(ChangeType.Removed, result[0].ChangeType);
        Assert.Equal("System.String.OldMethod", result[0].ElementName);
    }

    [Fact]
    public void CompareMembers_DetectsModifiedMembers()
    {
        // Arrange
        var oldType = typeof(string);
        var newType = typeof(string);

        var oldMember = new ApiMember
        {
            Name = "Method",
            FullName = "System.String.Method",
            Signature = "public void Method()",
            Type = MemberType.Method
        };

        var newMember = new ApiMember
        {
            Name = "Method",
            FullName = "System.String.Method",
            Signature = "public void Method()",
            Type = MemberType.Method
        };

        var oldMembers = new List<ApiMember> { oldMember };
        var newMembers = new List<ApiMember> { newMember };

        _mockApiExtractor.Setup(x => x.ExtractTypeMembers(oldType)).Returns(oldMembers);
        _mockApiExtractor.Setup(x => x.ExtractTypeMembers(newType)).Returns(newMembers);

        var modifiedMemberDifference = new ApiDifference
        {
            ChangeType = ChangeType.Modified,
            ElementType = ApiElementType.Method,
            ElementName = "System.String.Method",
            Description = "Modified method 'System.String.Method'",
            IsBreakingChange = true
        };

        _mockDifferenceCalculator.Setup(x => x.CalculateMemberChanges(It.Is<ApiMember>(m => m.Signature == oldMember.Signature),
                                                                     It.Is<ApiMember>(m => m.Signature == newMember.Signature)))
            .Returns(modifiedMemberDifference);

        // Act
        var result = _apiComparer.CompareMembers(oldType, newType).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(ChangeType.Modified, result[0].ChangeType);
        Assert.Equal("System.String.Method", result[0].ElementName);
    }
}

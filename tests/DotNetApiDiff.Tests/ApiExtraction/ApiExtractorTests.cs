// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using System.Reflection;
using DotNetApiDiff.ApiExtraction;
using DotNetApiDiff.Interfaces;
using DotNetApiDiff.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotNetApiDiff.Tests.ApiExtraction;

public class ApiExtractorTests
{
    private readonly Mock<ITypeAnalyzer> _mockTypeAnalyzer;
    private readonly Mock<ILogger<ApiExtractor>> _mockLogger;
    private readonly ApiExtractor _apiExtractor;

    public ApiExtractorTests()
    {
        _mockTypeAnalyzer = new Mock<ITypeAnalyzer>();
        _mockLogger = new Mock<ILogger<ApiExtractor>>();
        _apiExtractor = new ApiExtractor(_mockTypeAnalyzer.Object, _mockLogger.Object);
    }

    [Fact]
    public void ExtractApiMembers_WithValidAssembly_ReturnsApiMembers()
    {
        // Arrange
        var assembly = typeof(List<>).Assembly;
        var listType = typeof(List<>);
        var dictionaryType = typeof(Dictionary<,>);

        var types = new[] { listType, dictionaryType };

        var listTypeMember = new ApiMember { Name = "List`1", FullName = "System.Collections.Generic.List`1" };
        var dictionaryTypeMember = new ApiMember { Name = "Dictionary`2", FullName = "System.Collections.Generic.Dictionary`2" };

        var listMembers = new List<ApiMember>
        {
            new ApiMember { Name = "Add", FullName = "System.Collections.Generic.List`1.Add" },
            new ApiMember { Name = "Count", FullName = "System.Collections.Generic.List`1.Count" }
        };

        var dictionaryMembers = new List<ApiMember>
        {
            new ApiMember { Name = "Add", FullName = "System.Collections.Generic.Dictionary`2.Add" },
            new ApiMember { Name = "ContainsKey", FullName = "System.Collections.Generic.Dictionary`2.ContainsKey" }
        };

        // Setup mocks
        _mockTypeAnalyzer.Setup(x => x.AnalyzeType(listType)).Returns(listTypeMember);
        _mockTypeAnalyzer.Setup(x => x.AnalyzeType(dictionaryType)).Returns(dictionaryTypeMember);
        _mockTypeAnalyzer.Setup(x => x.AnalyzeMethods(listType)).Returns(new[] { listMembers[0] });
        _mockTypeAnalyzer.Setup(x => x.AnalyzeProperties(listType)).Returns(new[] { listMembers[1] });
        _mockTypeAnalyzer.Setup(x => x.AnalyzeFields(listType)).Returns(Enumerable.Empty<ApiMember>());
        _mockTypeAnalyzer.Setup(x => x.AnalyzeEvents(listType)).Returns(Enumerable.Empty<ApiMember>());
        _mockTypeAnalyzer.Setup(x => x.AnalyzeConstructors(listType)).Returns(Enumerable.Empty<ApiMember>());

        _mockTypeAnalyzer.Setup(x => x.AnalyzeMethods(dictionaryType)).Returns(dictionaryMembers);
        _mockTypeAnalyzer.Setup(x => x.AnalyzeProperties(dictionaryType)).Returns(Enumerable.Empty<ApiMember>());
        _mockTypeAnalyzer.Setup(x => x.AnalyzeFields(dictionaryType)).Returns(Enumerable.Empty<ApiMember>());
        _mockTypeAnalyzer.Setup(x => x.AnalyzeEvents(dictionaryType)).Returns(Enumerable.Empty<ApiMember>());
        _mockTypeAnalyzer.Setup(x => x.AnalyzeConstructors(dictionaryType)).Returns(Enumerable.Empty<ApiMember>());

        // Create a partial mock to override GetPublicTypes
        var partialMock = new Mock<ApiExtractor>(_mockTypeAnalyzer.Object, _mockLogger.Object) { CallBase = true };
        partialMock.Setup(x => x.GetPublicTypes(assembly)).Returns(types);

        // Act
        var result = partialMock.Object.ExtractApiMembers(assembly).ToList();

        // Assert
        Assert.Equal(6, result.Count); // 2 types + 4 members
        Assert.Contains(result, m => m.Name == "List`1");
        Assert.Contains(result, m => m.Name == "Dictionary`2");
        Assert.Contains(result, m => m.FullName == "System.Collections.Generic.List`1.Add");
        Assert.Contains(result, m => m.FullName == "System.Collections.Generic.List`1.Count");
        Assert.Contains(result, m => m.FullName == "System.Collections.Generic.Dictionary`2.Add");
        Assert.Contains(result, m => m.FullName == "System.Collections.Generic.Dictionary`2.ContainsKey");
    }

    [Fact]
    public void ExtractTypeMembers_WithValidType_ReturnsMembers()
    {
        // Arrange
        var type = typeof(List<>);

        var methodMembers = new[]
        {
            new ApiMember { Name = "Add", FullName = "System.Collections.Generic.List`1.Add", Type = MemberType.Method }
        };

        var propertyMembers = new[]
        {
            new ApiMember { Name = "Count", FullName = "System.Collections.Generic.List`1.Count", Type = MemberType.Property }
        };

        var fieldMembers = new[]
        {
            new ApiMember { Name = "_items", FullName = "System.Collections.Generic.List`1._items", Type = MemberType.Field }
        };

        var eventMembers = new[]
        {
            new ApiMember { Name = "Changed", FullName = "System.Collections.Generic.List`1.Changed", Type = MemberType.Event }
        };

        var constructorMembers = new[]
        {
            new ApiMember { Name = ".ctor", FullName = "System.Collections.Generic.List`1..ctor", Type = MemberType.Constructor }
        };

        // Setup mocks
        _mockTypeAnalyzer.Setup(x => x.AnalyzeMethods(type)).Returns(methodMembers);
        _mockTypeAnalyzer.Setup(x => x.AnalyzeProperties(type)).Returns(propertyMembers);
        _mockTypeAnalyzer.Setup(x => x.AnalyzeFields(type)).Returns(fieldMembers);
        _mockTypeAnalyzer.Setup(x => x.AnalyzeEvents(type)).Returns(eventMembers);
        _mockTypeAnalyzer.Setup(x => x.AnalyzeConstructors(type)).Returns(constructorMembers);

        // Act
        var result = _apiExtractor.ExtractTypeMembers(type).ToList();

        // Assert
        Assert.Equal(5, result.Count);
        Assert.Contains(result, m => m.Name == "Add" && m.Type == MemberType.Method);
        Assert.Contains(result, m => m.Name == "Count" && m.Type == MemberType.Property);
        Assert.Contains(result, m => m.Name == "_items" && m.Type == MemberType.Field);
        Assert.Contains(result, m => m.Name == "Changed" && m.Type == MemberType.Event);
        Assert.Contains(result, m => m.Name == ".ctor" && m.Type == MemberType.Constructor);
    }

    [Fact]
    public void GetPublicTypes_WithValidAssembly_ReturnsPublicTypes()
    {
        // Arrange
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();

        // Act
        var result = _apiExtractor.GetPublicTypes(assembly).ToList();

        // Assert
        Assert.NotEmpty(result);
        Assert.All(result, type => Assert.True(type.IsPublic || type.IsNestedPublic));
        Assert.DoesNotContain(result, type => type.Name.Contains('<')); // No compiler-generated types
    }

    [Fact]
    public void ExtractApiMembers_WithNullAssembly_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _apiExtractor.ExtractApiMembers(null!));
    }

    [Fact]
    public void ExtractTypeMembers_WithNullType_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _apiExtractor.ExtractTypeMembers(null!));
    }

    [Fact]
    public void GetPublicTypes_WithNullAssembly_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _apiExtractor.GetPublicTypes(null!));
    }
}

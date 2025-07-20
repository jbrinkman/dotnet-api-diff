// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using DotNetApiDiff.ApiExtraction;
using DotNetApiDiff.Models.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotNetApiDiff.Tests.ApiExtraction;

public class NameMapperTests
{
    private readonly Mock<ILogger<NameMapper>> _loggerMock;

    public NameMapperTests()
    {
        _loggerMock = new Mock<ILogger<NameMapper>>();
    }

    [Fact]
    public void MapNamespace_WithExactMatch_ReturnsCorrectMapping()
    {
        // Arrange
        var config = new MappingConfiguration
        {
            NamespaceMappings = new Dictionary<string, List<string>>
            {
                { "OldCompany.Product", new List<string> { "NewCompany.Product" } }
            }
        };
        var nameMapper = new NameMapper(config, _loggerMock.Object);

        // Act
        var result = nameMapper.MapNamespace("OldCompany.Product").ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("NewCompany.Product", result[0]);
    }

    [Fact]
    public void MapNamespace_WithPrefixMatch_ReturnsCorrectMapping()
    {
        // Arrange
        var config = new MappingConfiguration
        {
            NamespaceMappings = new Dictionary<string, List<string>>
            {
                { "OldCompany", new List<string> { "NewCompany" } }
            }
        };
        var nameMapper = new NameMapper(config, _loggerMock.Object);

        // Act
        var result = nameMapper.MapNamespace("OldCompany.Product.Feature").ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("NewCompany.Product.Feature", result[0]);
    }

    [Fact]
    public void MapNamespace_WithNoMatch_ReturnsOriginal()
    {
        // Arrange
        var config = new MappingConfiguration
        {
            NamespaceMappings = new Dictionary<string, List<string>>
            {
                { "OldCompany", new List<string> { "NewCompany" } }
            }
        };
        var nameMapper = new NameMapper(config, _loggerMock.Object);

        // Act
        var result = nameMapper.MapNamespace("DifferentCompany.Product").ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("DifferentCompany.Product", result[0]);
    }

    [Fact]
    public void MapNamespace_WithOneToManyMapping_ReturnsAllMappings()
    {
        // Arrange
        var config = new MappingConfiguration
        {
            NamespaceMappings = new Dictionary<string, List<string>>
            {
                { "OldCompany.Product", new List<string> { "NewCompany.Product", "AnotherCompany.Product" } }
            }
        };
        var nameMapper = new NameMapper(config, _loggerMock.Object);

        // Act
        var result = nameMapper.MapNamespace("OldCompany.Product").ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("NewCompany.Product", result);
        Assert.Contains("AnotherCompany.Product", result);
    }

    [Fact]
    public void MapNamespace_WithOneToManyPrefixMapping_ReturnsAllMappings()
    {
        // Arrange
        var config = new MappingConfiguration
        {
            NamespaceMappings = new Dictionary<string, List<string>>
            {
                { "OldCompany", new List<string> { "NewCompany", "AnotherCompany" } }
            }
        };
        var nameMapper = new NameMapper(config, _loggerMock.Object);

        // Act
        var result = nameMapper.MapNamespace("OldCompany.Product.Feature").ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("NewCompany.Product.Feature", result);
        Assert.Contains("AnotherCompany.Product.Feature", result);
    }

    [Fact]
    public void MapTypeName_WithExactMatch_ReturnsCorrectMapping()
    {
        // Arrange
        var config = new MappingConfiguration
        {
            TypeMappings = new Dictionary<string, string>
            {
                { "OldType", "NewType" }
            }
        };
        var nameMapper = new NameMapper(config, _loggerMock.Object);

        // Act
        var result = nameMapper.MapTypeName("OldType");

        // Assert
        Assert.Equal("NewType", result);
    }

    [Fact]
    public void MapTypeName_WithNoMatch_ReturnsOriginal()
    {
        // Arrange
        var config = new MappingConfiguration
        {
            TypeMappings = new Dictionary<string, string>
            {
                { "OldType", "NewType" }
            }
        };
        var nameMapper = new NameMapper(config, _loggerMock.Object);

        // Act
        var result = nameMapper.MapTypeName("DifferentType");

        // Assert
        Assert.Equal("DifferentType", result);
    }

    [Fact]
    public void MapFullTypeName_WithExactTypeMapping_ReturnsCorrectMapping()
    {
        // Arrange
        var config = new MappingConfiguration
        {
            TypeMappings = new Dictionary<string, string>
            {
                { "OldCompany.Product.OldType", "NewCompany.Product.NewType" }
            }
        };
        var nameMapper = new NameMapper(config, _loggerMock.Object);

        // Act
        var result = nameMapper.MapFullTypeName("OldCompany.Product.OldType").ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("NewCompany.Product.NewType", result[0]);
    }

    [Fact]
    public void MapFullTypeName_WithNamespaceAndTypeMapping_ReturnsCorrectMapping()
    {
        // Arrange
        var config = new MappingConfiguration
        {
            NamespaceMappings = new Dictionary<string, List<string>>
            {
                { "OldCompany.Product", new List<string> { "NewCompany.Product" } }
            },
            TypeMappings = new Dictionary<string, string>
            {
                { "OldType", "NewType" }
            }
        };
        var nameMapper = new NameMapper(config, _loggerMock.Object);

        // Act
        var result = nameMapper.MapFullTypeName("OldCompany.Product.OldType").ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("NewCompany.Product.NewType", result[0]);
    }

    [Fact]
    public void MapFullTypeName_WithOneToManyNamespaceMapping_ReturnsAllMappings()
    {
        // Arrange
        var config = new MappingConfiguration
        {
            NamespaceMappings = new Dictionary<string, List<string>>
            {
                { "OldCompany.Product", new List<string> { "NewCompany.Product", "AnotherCompany.Product" } }
            }
        };
        var nameMapper = new NameMapper(config, _loggerMock.Object);

        // Act
        var result = nameMapper.MapFullTypeName("OldCompany.Product.MyType").ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("NewCompany.Product.MyType", result);
        Assert.Contains("AnotherCompany.Product.MyType", result);
    }

    [Fact]
    public void ShouldAutoMapType_WhenAutoMapEnabled_ReturnsTrue()
    {
        // Arrange
        var config = new MappingConfiguration
        {
            AutoMapSameNameTypes = true
        };
        var nameMapper = new NameMapper(config, _loggerMock.Object);

        // Act
        var result = nameMapper.ShouldAutoMapType("OldCompany.Product.MyType");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldAutoMapType_WhenAutoMapDisabled_ReturnsFalse()
    {
        // Arrange
        var config = new MappingConfiguration
        {
            AutoMapSameNameTypes = false
        };
        var nameMapper = new NameMapper(config, _loggerMock.Object);

        // Act
        var result = nameMapper.ShouldAutoMapType("OldCompany.Product.MyType");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldAutoMapType_WithGenericType_ReturnsFalse()
    {
        // Arrange
        var config = new MappingConfiguration
        {
            AutoMapSameNameTypes = true
        };
        var nameMapper = new NameMapper(config, _loggerMock.Object);

        // Act
        var result = nameMapper.ShouldAutoMapType("OldCompany.Product.MyType`1");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void MapNamespace_WithIgnoreCase_ReturnsCorrectMapping()
    {
        // Arrange
        var config = new MappingConfiguration
        {
            NamespaceMappings = new Dictionary<string, List<string>>
            {
                { "oldcompany.product", new List<string> { "NewCompany.Product" } }
            },
            IgnoreCase = true
        };
        var nameMapper = new NameMapper(config, _loggerMock.Object);

        // Act
        var result = nameMapper.MapNamespace("OldCompany.Product").ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("NewCompany.Product", result[0]);
    }

    [Fact]
    public void MapTypeName_WithIgnoreCase_ReturnsCorrectMapping()
    {
        // Arrange
        var config = new MappingConfiguration
        {
            TypeMappings = new Dictionary<string, string>
            {
                { "oldtype", "NewType" }
            },
            IgnoreCase = true
        };
        var nameMapper = new NameMapper(config, _loggerMock.Object);

        // Act
        var result = nameMapper.MapTypeName("OldType");

        // Assert
        Assert.Equal("NewType", result);
    }
}

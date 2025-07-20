// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using DotNetApiDiff.ApiExtraction;
using DotNetApiDiff.Interfaces;
using DotNetApiDiff.Models;
using DotNetApiDiff.Models.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;
using Xunit;

// Test classes for the tests
namespace TestOldNamespace
{
    public class TestClass { }
}

namespace TestNewNamespace
{
    public class TestClass { }
}

namespace TestNewNamespace1
{
    public class TestClass { }
}

namespace TestNewNamespace2
{
    public class TestClass { }
}

namespace TestNamespace
{
    public class OldClassName { }
    public class NewClassName { }
}

namespace DotNetApiDiff.Tests.ApiExtraction
{
    public class ApiComparerWithMappingTests
    {
        private readonly Mock<IApiExtractor> _apiExtractorMock;
        private readonly Mock<IDifferenceCalculator> _differenceCalculatorMock;
        private readonly Mock<IChangeClassifier> _changeClassifierMock;
        private readonly Mock<ILogger<ApiComparer>> _loggerMock;
        private readonly Mock<ILogger<NameMapper>> _nameMapperLoggerMock;

        public ApiComparerWithMappingTests()
        {
            _apiExtractorMock = new Mock<IApiExtractor>();
            _differenceCalculatorMock = new Mock<IDifferenceCalculator>();
            _changeClassifierMock = new Mock<IChangeClassifier>();
            _loggerMock = new Mock<ILogger<ApiComparer>>();
            _nameMapperLoggerMock = new Mock<ILogger<NameMapper>>();

            // Setup default behavior for all tests
            _apiExtractorMock.Setup(x => x.ExtractTypeMembers(It.IsAny<Type>())).Returns(new List<ApiMember>());
            _differenceCalculatorMock.Setup(x => x.CalculateTypeChanges(It.IsAny<Type>(), It.IsAny<Type>())).Returns((ApiDifference?)null);

            // Setup the change classifier to return the same difference that is passed to it
            _changeClassifierMock.Setup(x => x.ClassifyChange(It.IsAny<ApiDifference>()))
                .Returns<ApiDifference>(diff => diff);
        }

        [Fact]
        public void MapNamespace_WithExactMatch_ReturnsCorrectMapping()
        {
            // Arrange
            var config = new MappingConfiguration
            {
                NamespaceMappings = new Dictionary<string, List<string>>
                {
                    { "TestOldNamespace", new List<string> { "TestNewNamespace" } }
                }
            };
            var nameMapper = new NameMapper(config, _nameMapperLoggerMock.Object);

            // Act
            var result = nameMapper.MapNamespace("TestOldNamespace").ToList();

            // Assert
            Assert.Single(result);
            Assert.Equal("TestNewNamespace", result[0]);
        }

        [Fact]
        public void MapTypeName_WithExactMatch_ReturnsCorrectMapping()
        {
            // Arrange
            var config = new MappingConfiguration
            {
                TypeMappings = new Dictionary<string, string>
                {
                    { "TestNamespace.OldClassName", "TestNamespace.NewClassName" }
                }
            };
            var nameMapper = new NameMapper(config, _nameMapperLoggerMock.Object);

            // Act
            var result = nameMapper.MapTypeName("TestNamespace.OldClassName");

            // Assert
            Assert.Equal("TestNamespace.NewClassName", result);
        }

        [Fact]
        public void MapFullTypeName_WithOneToManyNamespaceMapping_ReturnsAllMappings()
        {
            // Arrange
            var config = new MappingConfiguration
            {
                NamespaceMappings = new Dictionary<string, List<string>>
                {
                    { "TestOldNamespace", new List<string> { "TestNewNamespace1", "TestNewNamespace2" } }
                }
            };
            var nameMapper = new NameMapper(config, _nameMapperLoggerMock.Object);

            // Act
            var result = nameMapper.MapFullTypeName("TestOldNamespace.TestClass").ToList();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains("TestNewNamespace1.TestClass", result);
            Assert.Contains("TestNewNamespace2.TestClass", result);
        }

        [Fact]
        public void ShouldAutoMapType_WhenAutoMapEnabled_ReturnsTrue()
        {
            // Arrange
            var config = new MappingConfiguration
            {
                AutoMapSameNameTypes = true
            };
            var nameMapper = new NameMapper(config, _nameMapperLoggerMock.Object);

            // Act
            var result = nameMapper.ShouldAutoMapType("TestOldNamespace.TestClass");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CompareTypes_WithNoMapping_FindsDifferences()
        {
            // Arrange
            var oldType = typeof(TestOldNamespace.TestClass);
            var newType = typeof(TestNewNamespace.TestClass);

            var mappingConfig = new MappingConfiguration(); // No mappings

            var nameMapper = new NameMapper(mappingConfig, _nameMapperLoggerMock.Object);
            var apiComparer = new ApiComparer(_apiExtractorMock.Object, _differenceCalculatorMock.Object, nameMapper, _changeClassifierMock.Object, _loggerMock.Object);

            var addedDifference = new ApiDifference { ChangeType = ChangeType.Added };
            var removedDifference = new ApiDifference { ChangeType = ChangeType.Removed };

            _differenceCalculatorMock.Setup(x => x.CalculateAddedType(It.IsAny<Type>())).Returns(addedDifference);
            _differenceCalculatorMock.Setup(x => x.CalculateRemovedType(It.IsAny<Type>())).Returns(removedDifference);

            // Act
            var result = apiComparer.CompareTypes(new[] { oldType }, new[] { newType }).ToList();

            // Assert
            Assert.Equal(2, result.Count); // Should find one added and one removed type
            Assert.Contains(result, d => d.ChangeType == ChangeType.Added);
            Assert.Contains(result, d => d.ChangeType == ChangeType.Removed);
        }
    }
}

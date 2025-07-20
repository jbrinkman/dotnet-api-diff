// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using DotNetApiDiff.ApiExtraction;
using DotNetApiDiff.Interfaces;
using DotNetApiDiff.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace DotNetApiDiff.Tests.ApiExtraction;

public class ApiComparerManualTests
{
    [Fact]
    public void CompareMembers_DetectsRemovedMembers_ManualTest()
    {
        // Arrange
        Console.WriteLine("Starting CompareMembers_DetectsRemovedMembers_ManualTest");

        // Create test data - use different types to ensure they are treated as distinct objects
        var oldType = typeof(string);
        var newType = typeof(int); // Use a different type than oldType

        var oldMember = new ApiMember
        {
            Name = "OldMethod",
            FullName = "System.String.OldMethod",
            Signature = "public void OldMethod()",
            Type = MemberType.Method
        };

        // Create manual mocks
        var mockApiExtractor = new Mock<IApiExtractor>();
        mockApiExtractor.Setup(x => x.ExtractTypeMembers(oldType))
            .Returns(new List<ApiMember> { oldMember });
        mockApiExtractor.Setup(x => x.ExtractTypeMembers(newType))
            .Returns(new List<ApiMember>());

        var expectedDifference = new ApiDifference
        {
            ChangeType = ChangeType.Removed,
            ElementType = ApiElementType.Method,
            ElementName = "System.String.OldMethod",
            Description = "Removed method 'System.String.OldMethod'",
            IsBreakingChange = true
        };

        var mockDiffCalc = new Mock<IDifferenceCalculator>();
        mockDiffCalc.Setup(x => x.CalculateRemovedMember(It.Is<ApiMember>(m => m.Signature == oldMember.Signature)))
            .Returns(expectedDifference);

        var logger = new NullLogger<ApiComparer>();

        // Create a mock NameMapper
        var mockNameMapper = new Mock<INameMapper>();

        // Create a mock ChangeClassifier
        var mockChangeClassifier = new Mock<IChangeClassifier>();
        mockChangeClassifier.Setup(x => x.ClassifyChange(It.IsAny<ApiDifference>()))
            .Returns<ApiDifference>(diff => diff);

        // Create the comparer with our manual mocks
        var apiComparer = new ApiComparer(mockApiExtractor.Object, mockDiffCalc.Object, mockNameMapper.Object, mockChangeClassifier.Object, logger);

        // Act
        Console.WriteLine("About to call CompareMembers");
        var result = apiComparer.CompareMembers(oldType, newType).ToList();
        Console.WriteLine($"CompareMembers returned {result.Count} results");

        // Assert
        Assert.Single(result);
        Assert.Equal(ChangeType.Removed, result[0].ChangeType);
        Assert.Equal("System.String.OldMethod", result[0].ElementName);
    }
}

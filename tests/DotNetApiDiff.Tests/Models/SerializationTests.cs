using DotNetApiDiff.Models;
using System.Text.Json;
using Xunit;

namespace DotNetApiDiff.Tests.Models;

public class SerializationTests
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    [Fact]
    public void ApiMember_CanSerializeToJson()
    {
        // Arrange
        var member = new ApiMember
        {
            Name = "TestMethod",
            FullName = "TestNamespace.TestClass.TestMethod",
            Type = MemberType.Method,
            Accessibility = AccessibilityLevel.Public,
            Signature = "public void TestMethod(int param)",
            DeclaringType = "TestClass",
            Namespace = "TestNamespace",
            Attributes = { "System.ObsoleteAttribute", "System.ComponentModel.EditorBrowsableAttribute" }
        };

        // Act
        var json = JsonSerializer.Serialize(member, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<ApiMember>(json, _jsonOptions);

        // Assert
        Assert.NotNull(json);
        Assert.NotNull(deserialized);
        Assert.Equal(member.Name, deserialized.Name);
        Assert.Equal(member.FullName, deserialized.FullName);
        Assert.Equal(member.Type, deserialized.Type);
        Assert.Equal(member.Accessibility, deserialized.Accessibility);
        Assert.Equal(member.Signature, deserialized.Signature);
        Assert.Equal(member.DeclaringType, deserialized.DeclaringType);
        Assert.Equal(member.Namespace, deserialized.Namespace);
        Assert.Equal(member.Attributes.Count, deserialized.Attributes.Count);
        Assert.All(member.Attributes, attr => Assert.Contains(attr, deserialized.Attributes));
    }

    [Fact]
    public void ApiChange_CanSerializeToJson()
    {
        // Arrange
        var change = new ApiChange
        {
            Type = ChangeType.Modified,
            IsBreakingChange = true,
            Description = "Method signature changed",
            SourceMember = new ApiMember
            {
                Name = "TestMethod",
                FullName = "Test.TestMethod",
                Signature = "void TestMethod(int param)",
                Type = MemberType.Method,
                Accessibility = AccessibilityLevel.Public
            },
            TargetMember = new ApiMember
            {
                Name = "TestMethod",
                FullName = "Test.TestMethod",
                Signature = "int TestMethod(string param)",
                Type = MemberType.Method,
                Accessibility = AccessibilityLevel.Public
            },
            Details = { "Parameter type changed from int to string", "Return type changed from void to int" }
        };

        // Act
        var json = JsonSerializer.Serialize(change, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<ApiChange>(json, _jsonOptions);

        // Assert
        Assert.NotNull(json);
        Assert.NotNull(deserialized);
        Assert.Equal(change.Type, deserialized.Type);
        Assert.Equal(change.IsBreakingChange, deserialized.IsBreakingChange);
        Assert.Equal(change.Description, deserialized.Description);
        Assert.NotNull(deserialized.SourceMember);
        Assert.NotNull(deserialized.TargetMember);
        Assert.Equal(change.SourceMember.FullName, deserialized.SourceMember.FullName);
        Assert.Equal(change.TargetMember.FullName, deserialized.TargetMember.FullName);
        Assert.Equal(change.Details.Count, deserialized.Details.Count);
        Assert.All(change.Details, detail => Assert.Contains(detail, deserialized.Details));
    }

    [Fact]
    public void ApiComparison_CanSerializeToJson()
    {
        // Arrange
        var comparison = new ApiComparison();

        comparison.Additions.Add(new ApiChange
        {
            Type = ChangeType.Added,
            Description = "Added new method",
            TargetMember = new ApiMember
            {
                Name = "NewMethod",
                FullName = "Test.NewMethod",
                Signature = "void NewMethod()",
                Type = MemberType.Method,
                Accessibility = AccessibilityLevel.Public
            }
        });

        comparison.Removals.Add(new ApiChange
        {
            Type = ChangeType.Removed,
            Description = "Removed old method",
            IsBreakingChange = true,
            SourceMember = new ApiMember
            {
                Name = "OldMethod",
                FullName = "Test.OldMethod",
                Signature = "void OldMethod()",
                Type = MemberType.Method,
                Accessibility = AccessibilityLevel.Public
            }
        });

        comparison.UpdateSummary();

        // Act
        var json = JsonSerializer.Serialize(comparison, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<ApiComparison>(json, _jsonOptions);

        // Assert
        Assert.NotNull(json);
        Assert.NotNull(deserialized);
        Assert.Equal(comparison.Additions.Count, deserialized.Additions.Count);
        Assert.Equal(comparison.Removals.Count, deserialized.Removals.Count);
        Assert.Equal(comparison.Modifications.Count, deserialized.Modifications.Count);
        Assert.Equal(comparison.Excluded.Count, deserialized.Excluded.Count);
        Assert.Equal(comparison.Summary.AddedCount, deserialized.Summary.AddedCount);
        Assert.Equal(comparison.Summary.RemovedCount, deserialized.Summary.RemovedCount);
        Assert.Equal(comparison.Summary.BreakingChangesCount, deserialized.Summary.BreakingChangesCount);
    }

    [Fact]
    public void Enums_SerializeAsNumbers()
    {
        // Arrange
        var member = new ApiMember
        {
            Name = "Test",
            FullName = "Test.Test",
            Signature = "void Test()",
            Type = MemberType.Method,
            Accessibility = AccessibilityLevel.Public
        };

        // Act
        var json = JsonSerializer.Serialize(member, _jsonOptions);

        // Assert
        Assert.Contains($"\"type\": {Convert.ToInt32(MemberType.Method)}", json); // Dynamically retrieve enum value
        Assert.Contains($"\"accessibility\": {Convert.ToInt32(AccessibilityLevel.Public)}", json); // Dynamically retrieve enum value
    }

    [Fact]
    public void EmptyCollections_SerializeCorrectly()
    {
        // Arrange
        var comparison = new ApiComparison();

        // Act
        var json = JsonSerializer.Serialize(comparison, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<ApiComparison>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.NotNull(deserialized.Additions);
        Assert.NotNull(deserialized.Removals);
        Assert.NotNull(deserialized.Modifications);
        Assert.NotNull(deserialized.Excluded);
        Assert.Empty(deserialized.Additions);
        Assert.Empty(deserialized.Removals);
        Assert.Empty(deserialized.Modifications);
        Assert.Empty(deserialized.Excluded);
    }
}
using DotNetApiDiff.Models.Configuration;
using System.Text.Json;
using Xunit;

namespace DotNetApiDiff.Tests.Models.Configuration;

public class ExclusionConfigurationTests
{
    [Fact]
    public void CreateDefault_ReturnsValidConfiguration()
    {
        // Act
        var config = ExclusionConfiguration.CreateDefault();

        // Assert
        Assert.NotNull(config);
        Assert.Empty(config.ExcludedTypes);
        Assert.Empty(config.ExcludedMembers);
        Assert.Empty(config.ExcludedTypePatterns);
        Assert.Empty(config.ExcludedMemberPatterns);
        Assert.True(config.ExcludeCompilerGenerated);
        Assert.False(config.ExcludeObsolete);
        Assert.True(config.IsValid());
    }

    [Fact]
    public void IsValid_WithValidConfiguration_ReturnsTrue()
    {
        // Arrange
        var config = new ExclusionConfiguration
        {
            ExcludedTypes = new List<string> { "System.Diagnostics.Debug", "System.Diagnostics.Trace" },
            ExcludedMembers = new List<string> { "System.Console.WriteLine", "System.Console.ReadLine" },
            ExcludedTypePatterns = new List<string> { "System.Diagnostics.*", "Microsoft.*.Internal" },
            ExcludedMemberPatterns = new List<string> { "*.ToString", "*.Equals" }
        };

        // Act & Assert
        Assert.True(config.IsValid());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void IsValid_WithEmptyExcludedType_ReturnsFalse(string? emptyValue)
    {
        // Arrange
        var config = new ExclusionConfiguration
        {
            ExcludedTypes = new List<string> { "System.Diagnostics.Debug", emptyValue! }
        };

        // Act & Assert
        Assert.False(config.IsValid());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void IsValid_WithEmptyExcludedMember_ReturnsFalse(string? emptyValue)
    {
        // Arrange
        var config = new ExclusionConfiguration
        {
            ExcludedMembers = new List<string> { "System.Console.WriteLine", emptyValue! }
        };

        // Act & Assert
        Assert.False(config.IsValid());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void IsValid_WithEmptyExcludedTypePattern_ReturnsFalse(string? emptyValue)
    {
        // Arrange
        var config = new ExclusionConfiguration
        {
            ExcludedTypePatterns = new List<string> { "System.Diagnostics.*", emptyValue! }
        };

        // Act & Assert
        Assert.False(config.IsValid());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void IsValid_WithEmptyExcludedMemberPattern_ReturnsFalse(string? emptyValue)
    {
        // Arrange
        var config = new ExclusionConfiguration
        {
            ExcludedMemberPatterns = new List<string> { "*.ToString", emptyValue! }
        };

        // Act & Assert
        Assert.False(config.IsValid());
    }

    [Fact]
    public void IsValid_WithValidWildcardPatterns_ReturnsTrue()
    {
        // Arrange
        var config = new ExclusionConfiguration
        {
            ExcludedTypePatterns = new List<string>
            {
                "System.*",
                "Microsoft.?",
                "*.Internal.*",
                "Test?Namespace.Test*Class"
            }
        };

        // Act & Assert
        Assert.True(config.IsValid());
    }

    [Fact]
    public void Serialization_RoundTrip_PreservesValues()
    {
        // Arrange
        var config = new ExclusionConfiguration
        {
            ExcludedTypes = new List<string> { "System.Diagnostics.Debug", "System.Diagnostics.Trace" },
            ExcludedMembers = new List<string> { "System.Console.WriteLine", "System.Console.ReadLine" },
            ExcludedTypePatterns = new List<string> { "System.Diagnostics.*", "Microsoft.*.Internal" },
            ExcludedMemberPatterns = new List<string> { "*.ToString", "*.Equals" },
            ExcludeCompilerGenerated = false,
            ExcludeObsolete = true
        };

        // Act
        var json = JsonSerializer.Serialize(config);
        var deserialized = JsonSerializer.Deserialize<ExclusionConfiguration>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(config.ExcludedTypes.Count, deserialized!.ExcludedTypes.Count);
        Assert.Equal(config.ExcludedMembers.Count, deserialized.ExcludedMembers.Count);
        Assert.Equal(config.ExcludedTypePatterns.Count, deserialized.ExcludedTypePatterns.Count);
        Assert.Equal(config.ExcludedMemberPatterns.Count, deserialized.ExcludedMemberPatterns.Count);
        Assert.Equal(config.ExcludeCompilerGenerated, deserialized.ExcludeCompilerGenerated);
        Assert.Equal(config.ExcludeObsolete, deserialized.ExcludeObsolete);

        Assert.All(config.ExcludedTypes, t => Assert.Contains(t, deserialized.ExcludedTypes));
        Assert.All(config.ExcludedMembers, m => Assert.Contains(m, deserialized.ExcludedMembers));
        Assert.All(config.ExcludedTypePatterns, p => Assert.Contains(p, deserialized.ExcludedTypePatterns));
        Assert.All(config.ExcludedMemberPatterns, p => Assert.Contains(p, deserialized.ExcludedMemberPatterns));
    }
}
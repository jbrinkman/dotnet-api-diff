using DotNetApiDiff.Models.Configuration;
using System.Text.Json;
using Xunit;

namespace DotNetApiDiff.Tests.Models.Configuration;

public class MappingConfigurationTests
{
    [Fact]
    public void CreateDefault_ReturnsValidConfiguration()
    {
        // Act
        var config = MappingConfiguration.CreateDefault();

        // Assert
        Assert.NotNull(config);
        Assert.Empty(config.NamespaceMappings);
        Assert.Empty(config.TypeMappings);
        Assert.False(config.AutoMapSameNameTypes);
        Assert.False(config.IgnoreCase);
        Assert.True(config.IsValid());
    }

    [Fact]
    public void IsValid_WithValidConfiguration_ReturnsTrue()
    {
        // Arrange
        var config = new MappingConfiguration
        {
            NamespaceMappings = new Dictionary<string, List<string>>
            {
                { "OldNamespace", new List<string> { "NewNamespace" } },
                { "OldNamespace.SubNamespace", new List<string> { "NewNamespace.SubNamespace" } }
            },
            TypeMappings = new Dictionary<string, string>
            {
                { "OldNamespace.OldType", "NewNamespace.NewType" },
                { "OldNamespace.SubNamespace.OldType", "NewNamespace.SubNamespace.NewType" }
            }
        };

        // Act & Assert
        Assert.True(config.IsValid());
    }

    [Fact]
    public void IsValid_WithEmptyNamespaceKey_ReturnsFalse()
    {
        // Arrange
        var config = new MappingConfiguration
        {
            NamespaceMappings = new Dictionary<string, List<string>>
            {
                { "", new List<string> { "NewNamespace" } }
            }
        };

        // Act & Assert
        Assert.False(config.IsValid());
    }

    [Fact]
    public void IsValid_WithNullNamespaceValues_ReturnsFalse()
    {
        // Arrange
        var config = new MappingConfiguration
        {
            NamespaceMappings = new Dictionary<string, List<string>>
            {
                { "OldNamespace", null! }
            }
        };

        // Act & Assert
        Assert.False(config.IsValid());
    }

    [Fact]
    public void IsValid_WithEmptyNamespaceValues_ReturnsFalse()
    {
        // Arrange
        var config = new MappingConfiguration
        {
            NamespaceMappings = new Dictionary<string, List<string>>
            {
                { "OldNamespace", new List<string>() }
            }
        };

        // Act & Assert
        Assert.False(config.IsValid());
    }

    [Fact]
    public void IsValid_WithEmptyNamespaceValueItem_ReturnsFalse()
    {
        // Arrange
        var config = new MappingConfiguration
        {
            NamespaceMappings = new Dictionary<string, List<string>>
            {
                { "OldNamespace", new List<string> { "" } }
            }
        };

        // Act & Assert
        Assert.False(config.IsValid());
    }

    [Fact]
    public void IsValid_WithEmptyTypeKey_ReturnsFalse()
    {
        // Arrange
        var config = new MappingConfiguration
        {
            TypeMappings = new Dictionary<string, string>
            {
                { "", "NewNamespace.NewType" }
            }
        };

        // Act & Assert
        Assert.False(config.IsValid());
    }

    [Fact]
    public void IsValid_WithEmptyTypeValue_ReturnsFalse()
    {
        // Arrange
        var config = new MappingConfiguration
        {
            TypeMappings = new Dictionary<string, string>
            {
                { "OldNamespace.OldType", "" }
            }
        };

        // Act & Assert
        Assert.False(config.IsValid());
    }

    [Fact]
    public void IsValid_WithCircularReferences_ReturnsFalse()
    {
        // Arrange
        var config = new MappingConfiguration
        {
            NamespaceMappings = new Dictionary<string, List<string>>
            {
                { "A", new List<string> { "B" } },
                { "B", new List<string> { "C" } },
                { "C", new List<string> { "A" } }
            }
        };

        // Act & Assert
        Assert.False(config.IsValid());
    }

    [Fact]
    public void IsValid_WithSelfReference_ReturnsFalse()
    {
        // Arrange
        var config = new MappingConfiguration
        {
            NamespaceMappings = new Dictionary<string, List<string>>
            {
                { "A", new List<string> { "A" } }
            }
        };

        // Act & Assert
        Assert.False(config.IsValid());
    }

    [Fact]
    public void IsValid_WithComplexButValidMappings_ReturnsTrue()
    {
        // Arrange
        var config = new MappingConfiguration
        {
            NamespaceMappings = new Dictionary<string, List<string>>
            {
                { "A", new List<string> { "B", "C" } },
                { "B", new List<string> { "D" } },
                { "C", new List<string> { "E" } },
                { "F", new List<string> { "G" } }
            }
        };

        // Act & Assert
        Assert.True(config.IsValid());
    }

    [Fact]
    public void Serialization_RoundTrip_PreservesValues()
    {
        // Arrange
        var config = new MappingConfiguration
        {
            NamespaceMappings = new Dictionary<string, List<string>>
            {
                { "OldNamespace", new List<string> { "NewNamespace" } },
                { "OldNamespace.SubNamespace", new List<string> { "NewNamespace.SubNamespace", "AnotherNamespace" } }
            },
            TypeMappings = new Dictionary<string, string>
            {
                { "OldNamespace.OldType", "NewNamespace.NewType" },
                { "OldNamespace.SubNamespace.OldType", "NewNamespace.SubNamespace.NewType" }
            },
            AutoMapSameNameTypes = true,
            IgnoreCase = true
        };

        // Act
        var json = JsonSerializer.Serialize(config);
        var deserialized = JsonSerializer.Deserialize<MappingConfiguration>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(config.NamespaceMappings.Count, deserialized!.NamespaceMappings.Count);
        Assert.Equal(config.TypeMappings.Count, deserialized.TypeMappings.Count);
        Assert.Equal(config.AutoMapSameNameTypes, deserialized.AutoMapSameNameTypes);
        Assert.Equal(config.IgnoreCase, deserialized.IgnoreCase);

        foreach (var kvp in config.NamespaceMappings)
        {
            Assert.True(deserialized.NamespaceMappings.ContainsKey(kvp.Key));
            Assert.Equal(kvp.Value.Count, deserialized.NamespaceMappings[kvp.Key].Count);
            Assert.All(kvp.Value, v => Assert.Contains(v, deserialized.NamespaceMappings[kvp.Key]));
        }

        foreach (var kvp in config.TypeMappings)
        {
            Assert.True(deserialized.TypeMappings.ContainsKey(kvp.Key));
            Assert.Equal(kvp.Value, deserialized.TypeMappings[kvp.Key]);
        }
    }
}

using DotNetApiDiff.Models.Configuration;
using System.Text.Json;
using Xunit;

namespace DotNetApiDiff.Tests.Models.Configuration;

public class BreakingChangeRulesTests
{
    [Fact]
    public void CreateDefault_ReturnsExpectedDefaults()
    {
        // Act
        var rules = BreakingChangeRules.CreateDefault();

        // Assert
        Assert.NotNull(rules);
        Assert.True(rules.TreatTypeRemovalAsBreaking);
        Assert.True(rules.TreatMemberRemovalAsBreaking);
        Assert.True(rules.TreatSignatureChangeAsBreaking);
        Assert.True(rules.TreatReducedAccessibilityAsBreaking);
        Assert.False(rules.TreatAddedInterfaceAsBreaking);
        Assert.True(rules.TreatRemovedInterfaceAsBreaking);
        Assert.False(rules.TreatParameterNameChangeAsBreaking);
        Assert.False(rules.TreatAddedOptionalParameterAsBreaking);
        Assert.False(rules.TreatAddedMemberAsBreaking);
        Assert.False(rules.TreatAddedTypeAsBreaking);
    }

    [Fact]
    public void Serialization_RoundTrip_PreservesValues()
    {
        // Arrange
        var rules = new BreakingChangeRules
        {
            TreatTypeRemovalAsBreaking = false,
            TreatMemberRemovalAsBreaking = false,
            TreatSignatureChangeAsBreaking = false,
            TreatReducedAccessibilityAsBreaking = false,
            TreatAddedInterfaceAsBreaking = true,
            TreatRemovedInterfaceAsBreaking = false,
            TreatParameterNameChangeAsBreaking = true,
            TreatAddedOptionalParameterAsBreaking = true,
            TreatAddedMemberAsBreaking = true,
            TreatAddedTypeAsBreaking = true
        };

        // Act
        var json = JsonSerializer.Serialize(rules);
        var deserialized = JsonSerializer.Deserialize<BreakingChangeRules>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(rules.TreatTypeRemovalAsBreaking, deserialized!.TreatTypeRemovalAsBreaking);
        Assert.Equal(rules.TreatMemberRemovalAsBreaking, deserialized.TreatMemberRemovalAsBreaking);
        Assert.Equal(rules.TreatSignatureChangeAsBreaking, deserialized.TreatSignatureChangeAsBreaking);
        Assert.Equal(rules.TreatReducedAccessibilityAsBreaking, deserialized.TreatReducedAccessibilityAsBreaking);
        Assert.Equal(rules.TreatAddedInterfaceAsBreaking, deserialized.TreatAddedInterfaceAsBreaking);
        Assert.Equal(rules.TreatRemovedInterfaceAsBreaking, deserialized.TreatRemovedInterfaceAsBreaking);
        Assert.Equal(rules.TreatParameterNameChangeAsBreaking, deserialized.TreatParameterNameChangeAsBreaking);
        Assert.Equal(rules.TreatAddedOptionalParameterAsBreaking, deserialized.TreatAddedOptionalParameterAsBreaking);
        Assert.Equal(rules.TreatAddedMemberAsBreaking, deserialized.TreatAddedMemberAsBreaking);
        Assert.Equal(rules.TreatAddedTypeAsBreaking, deserialized.TreatAddedTypeAsBreaking);
    }

    [Fact]
    public void JsonPropertyNames_AreCorrect()
    {
        // Arrange
        var rules = new BreakingChangeRules
        {
            TreatTypeRemovalAsBreaking = false,
            TreatMemberRemovalAsBreaking = false,
            TreatSignatureChangeAsBreaking = false,
            TreatReducedAccessibilityAsBreaking = false,
            TreatAddedInterfaceAsBreaking = true,
            TreatRemovedInterfaceAsBreaking = false,
            TreatParameterNameChangeAsBreaking = true,
            TreatAddedOptionalParameterAsBreaking = true,
            TreatAddedMemberAsBreaking = true,
            TreatAddedTypeAsBreaking = true
        };

        // Act
        var json = JsonSerializer.Serialize(rules);

        // Assert
        Assert.Contains("treatTypeRemovalAsBreaking", json);
        Assert.Contains("treatMemberRemovalAsBreaking", json);
        Assert.Contains("treatSignatureChangeAsBreaking", json);
        Assert.Contains("treatReducedAccessibilityAsBreaking", json);
        Assert.Contains("treatAddedInterfaceAsBreaking", json);
        Assert.Contains("treatRemovedInterfaceAsBreaking", json);
        Assert.Contains("treatParameterNameChangeAsBreaking", json);
        Assert.Contains("treatAddedOptionalParameterAsBreaking", json);
        Assert.Contains("treatAddedMemberAsBreaking", json);
        Assert.Contains("treatAddedTypeAsBreaking", json);
    }
}
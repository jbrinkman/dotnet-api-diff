using System.Text.Json.Serialization;

namespace DotNetApiDiff.Models.Configuration;

/// <summary>
/// Configuration for defining what constitutes breaking changes
/// </summary>
public class BreakingChangeRules
{
    /// <summary>
    /// Whether removing a public type is considered a breaking change
    /// </summary>
    [JsonPropertyName("treatTypeRemovalAsBreaking")]
    public bool TreatTypeRemovalAsBreaking { get; set; } = true;

    /// <summary>
    /// Whether removing a public member is considered a breaking change
    /// </summary>
    [JsonPropertyName("treatMemberRemovalAsBreaking")]
    public bool TreatMemberRemovalAsBreaking { get; set; } = true;

    /// <summary>
    /// Whether changing a member's signature is considered a breaking change
    /// </summary>
    [JsonPropertyName("treatSignatureChangeAsBreaking")]
    public bool TreatSignatureChangeAsBreaking { get; set; } = true;

    /// <summary>
    /// Whether reducing a member's accessibility is considered a breaking change
    /// </summary>
    [JsonPropertyName("treatReducedAccessibilityAsBreaking")]
    public bool TreatReducedAccessibilityAsBreaking { get; set; } = true;

    /// <summary>
    /// Whether adding an interface to a type is considered a breaking change
    /// </summary>
    [JsonPropertyName("treatAddedInterfaceAsBreaking")]
    public bool TreatAddedInterfaceAsBreaking { get; set; } = false;

    /// <summary>
    /// Whether removing an interface from a type is considered a breaking change
    /// </summary>
    [JsonPropertyName("treatRemovedInterfaceAsBreaking")]
    public bool TreatRemovedInterfaceAsBreaking { get; set; } = true;

    /// <summary>
    /// Whether changing a parameter name is considered a breaking change
    /// </summary>
    [JsonPropertyName("treatParameterNameChangeAsBreaking")]
    public bool TreatParameterNameChangeAsBreaking { get; set; } = false;

    /// <summary>
    /// Whether adding an optional parameter is considered a breaking change
    /// </summary>
    [JsonPropertyName("treatAddedOptionalParameterAsBreaking")]
    public bool TreatAddedOptionalParameterAsBreaking { get; set; } = false;

    /// <summary>
    /// Whether adding a new member is considered a breaking change
    /// </summary>
    [JsonPropertyName("treatAddedMemberAsBreaking")]
    public bool TreatAddedMemberAsBreaking { get; set; } = false;

    /// <summary>
    /// Whether adding a new type is considered a breaking change
    /// </summary>
    [JsonPropertyName("treatAddedTypeAsBreaking")]
    public bool TreatAddedTypeAsBreaking { get; set; } = false;

    /// <summary>
    /// Creates a default set of breaking change rules
    /// </summary>
    /// <returns>A default set of breaking change rules</returns>
    public static BreakingChangeRules CreateDefault()
    {
        return new BreakingChangeRules
        {
            TreatTypeRemovalAsBreaking = true,
            TreatMemberRemovalAsBreaking = true,
            TreatSignatureChangeAsBreaking = true,
            TreatReducedAccessibilityAsBreaking = true,
            TreatAddedInterfaceAsBreaking = false,
            TreatRemovedInterfaceAsBreaking = true,
            TreatParameterNameChangeAsBreaking = false,
            TreatAddedOptionalParameterAsBreaking = false,
            TreatAddedMemberAsBreaking = false,
            TreatAddedTypeAsBreaking = false
        };
    }
}
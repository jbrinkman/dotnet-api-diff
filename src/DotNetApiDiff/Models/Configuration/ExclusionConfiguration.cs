using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace DotNetApiDiff.Models.Configuration;

/// <summary>
/// Configuration for excluding types and members from API comparison
/// </summary>
public class ExclusionConfiguration
{
    /// <summary>
    /// List of fully qualified type names to exclude
    /// </summary>
    [JsonPropertyName("excludedTypes")]
    public List<string> ExcludedTypes { get; set; } = new();

    /// <summary>
    /// List of fully qualified member names to exclude
    /// </summary>
    [JsonPropertyName("excludedMembers")]
    public List<string> ExcludedMembers { get; set; } = new();

    /// <summary>
    /// List of type name patterns to exclude (supports wildcards * and ?)
    /// </summary>
    [JsonPropertyName("excludedTypePatterns")]
    public List<string> ExcludedTypePatterns { get; set; } = new();

    /// <summary>
    /// List of member name patterns to exclude (supports wildcards * and ?)
    /// </summary>
    [JsonPropertyName("excludedMemberPatterns")]
    public List<string> ExcludedMemberPatterns { get; set; } = new();

    /// <summary>
    /// Whether to exclude compiler-generated types and members
    /// </summary>
    [JsonPropertyName("excludeCompilerGenerated")]
    public bool ExcludeCompilerGenerated { get; set; } = true;

    /// <summary>
    /// Whether to exclude obsolete types and members
    /// </summary>
    [JsonPropertyName("excludeObsolete")]
    public bool ExcludeObsolete { get; set; } = false;

    /// <summary>
    /// Validates the exclusion configuration
    /// </summary>
    /// <returns>True if valid, false otherwise</returns>
    public bool IsValid()
    {
        // Check for empty entries
        if (ExcludedTypes.Any(string.IsNullOrWhiteSpace) ||
            ExcludedMembers.Any(string.IsNullOrWhiteSpace) ||
            ExcludedTypePatterns.Any(string.IsNullOrWhiteSpace) ||
            ExcludedMemberPatterns.Any(string.IsNullOrWhiteSpace))
        {
            return false;
        }

        // Validate patterns (should be valid wildcards)
        try
        {
            foreach (var pattern in ExcludedTypePatterns.Concat(ExcludedMemberPatterns))
            {
                // Convert wildcard pattern to regex for validation
                WildcardToRegex(pattern);
            }
        }
        catch (Exception ex)
        {
            // Log the exception details for debugging purposes
            Console.WriteLine($"Validation failed due to an exception: {ex.Message}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Converts a wildcard pattern to a regular expression
    /// </summary>
    /// <param name="pattern">The wildcard pattern</param>
    /// <returns>A regular expression pattern</returns>
    private static string WildcardToRegex(string pattern)
    {
        return "^" + Regex.Escape(pattern)
                          .Replace("\\*", ".*")
                          .Replace("\\?", ".") + "$";
    }

    /// <summary>
    /// Creates a default exclusion configuration
    /// </summary>
    /// <returns>A default exclusion configuration</returns>
    public static ExclusionConfiguration CreateDefault()
    {
        return new ExclusionConfiguration
        {
            ExcludedTypes = new List<string>(),
            ExcludedMembers = new List<string>(),
            ExcludedTypePatterns = new List<string>(),
            ExcludedMemberPatterns = new List<string>(),
            ExcludeCompilerGenerated = true,
            ExcludeObsolete = false
        };
    }
}
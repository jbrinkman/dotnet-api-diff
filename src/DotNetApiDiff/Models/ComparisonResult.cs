namespace DotNetApiDiff.Models;

/// <summary>
/// Represents the result of comparing two assemblies
/// </summary>
public class ComparisonResult
{
    /// <summary>
    /// Path to the original assembly
    /// </summary>
    public string OldAssemblyPath { get; set; } = string.Empty;
    
    /// <summary>
    /// Path to the new assembly
    /// </summary>
    public string NewAssemblyPath { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp when the comparison was performed
    /// </summary>
    public DateTime ComparisonTimestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// List of all detected API differences
    /// </summary>
    public List<ApiDifference> Differences { get; set; } = new();
    
    /// <summary>
    /// Summary statistics of the comparison
    /// </summary>
    public ComparisonSummary Summary { get; set; } = new();
    
    /// <summary>
    /// Gets whether any breaking changes were detected
    /// </summary>
    public bool HasBreakingChanges => Differences.Any(d => d.IsBreakingChange);
    
    /// <summary>
    /// Gets the total number of differences found
    /// </summary>
    public int TotalDifferences => Differences.Count;
}

/// <summary>
/// Summary statistics for a comparison result
/// </summary>
public class ComparisonSummary
{
    /// <summary>
    /// Number of added API elements
    /// </summary>
    public int AddedCount { get; set; }
    
    /// <summary>
    /// Number of removed API elements
    /// </summary>
    public int RemovedCount { get; set; }
    
    /// <summary>
    /// Number of modified API elements
    /// </summary>
    public int ModifiedCount { get; set; }
    
    /// <summary>
    /// Number of breaking changes
    /// </summary>
    public int BreakingChangesCount { get; set; }
    
    /// <summary>
    /// Total number of changes
    /// </summary>
    public int TotalChanges => AddedCount + RemovedCount + ModifiedCount;
}
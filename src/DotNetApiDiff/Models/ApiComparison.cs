namespace DotNetApiDiff.Models;

/// <summary>
/// Represents the complete comparison result between two API surfaces
/// </summary>
public class ApiComparison
{
    /// <summary>
    /// List of API members that were added
    /// </summary>
    public List<ApiChange> Additions { get; set; } = new List<ApiChange>();

    /// <summary>
    /// List of API members that were removed
    /// </summary>
    public List<ApiChange> Removals { get; set; } = new List<ApiChange>();

    /// <summary>
    /// List of API members that were modified
    /// </summary>
    public List<ApiChange> Modifications { get; set; } = new List<ApiChange>();

    /// <summary>
    /// List of API members that were intentionally excluded
    /// </summary>
    public List<ApiChange> Excluded { get; set; } = new List<ApiChange>();

    /// <summary>
    /// Summary statistics of the comparison
    /// </summary>
    public ComparisonSummary Summary { get; set; } = new ComparisonSummary();

    /// <summary>
    /// Gets all changes as a single collection
    /// </summary>
    public IEnumerable<ApiChange> AllChanges
    {
        get
        {
            return Additions.Concat(Removals).Concat(Modifications).Concat(Excluded);
        }
    }

    /// <summary>
    /// Gets whether any breaking changes were detected
    /// </summary>
    public bool HasBreakingChanges => AllChanges.Any(c => c.IsBreakingChange);

    /// <summary>
    /// Gets the total number of changes (excluding excluded items)
    /// </summary>
    public int TotalChanges => Additions.Count + Removals.Count + Modifications.Count;

    /// <summary>
    /// Gets the total number of breaking changes
    /// </summary>
    public int BreakingChangesCount => AllChanges.Count(c => c.IsBreakingChange);

    /// <summary>
    /// Validates the ApiComparison instance
    /// </summary>
    /// <returns>True if valid, false otherwise</returns>
    public bool IsValid()
    {
        // Validate all changes
        var allChanges = AllChanges.ToList();
        if (allChanges.Any(c => !c.IsValid()))
        {
            return false;
        }

        // Validate that additions only contain Added changes
        if (Additions.Any(c => c.Type != ChangeType.Added))
        {
            return false;
        }

        // Validate that removals only contain Removed changes
        if (Removals.Any(c => c.Type != ChangeType.Removed))
        {
            return false;
        }

        // Validate that modifications only contain Modified changes
        if (Modifications.Any(c => c.Type != ChangeType.Modified))
        {
            return false;
        }

        // Validate that excluded only contain Excluded changes
        if (Excluded.Any(c => c.Type != ChangeType.Excluded))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Updates the summary statistics based on current changes
    /// </summary>
    public void UpdateSummary()
    {
        Summary.AddedCount = Additions.Count;
        Summary.RemovedCount = Removals.Count;
        Summary.ModifiedCount = Modifications.Count;
        Summary.BreakingChangesCount = BreakingChangesCount;
    }
}
// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
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
    /// Gets the validation error message if the comparison is invalid, empty if valid
    /// </summary>
    public string InvalidMessage { get; private set; } = string.Empty;

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
        // Clear any previous validation message
        InvalidMessage = string.Empty;

        // Validate all changes
        var allChanges = AllChanges.ToList();
        var invalidChanges = allChanges.Where(c => !c.IsValid()).ToList();
        if (invalidChanges.Any())
        {
            InvalidMessage = $"Found {invalidChanges.Count} invalid change(s): {string.Join(", ", invalidChanges.Select(c => $"'{c.Description}' ({c.Type})"))}";
            return false;
        }

        // Validate that additions only contain Added changes
        var wrongAdditions = Additions.Where(c => c.Type != ChangeType.Added).ToList();
        if (wrongAdditions.Any())
        {
            InvalidMessage = $"Additions collection contains {wrongAdditions.Count} change(s) with wrong type: {string.Join(", ", wrongAdditions.Select(c => $"'{c.Description}' ({c.Type})"))}";
            return false;
        }

        // Validate that removals only contain Removed changes
        var wrongRemovals = Removals.Where(c => c.Type != ChangeType.Removed).ToList();
        if (wrongRemovals.Any())
        {
            InvalidMessage = $"Removals collection contains {wrongRemovals.Count} change(s) with wrong type: {string.Join(", ", wrongRemovals.Select(c => $"'{c.Description}' ({c.Type})"))}";
            return false;
        }

        // Validate that modifications only contain Modified changes
        var wrongModifications = Modifications.Where(c => c.Type != ChangeType.Modified).ToList();
        if (wrongModifications.Any())
        {
            InvalidMessage = $"Modifications collection contains {wrongModifications.Count} change(s) with wrong type: {string.Join(", ", wrongModifications.Select(c => $"'{c.Description}' ({c.Type})"))}";
            return false;
        }

        // Validate that excluded only contain Excluded changes
        var wrongExcluded = Excluded.Where(c => c.Type != ChangeType.Excluded).ToList();
        if (wrongExcluded.Any())
        {
            InvalidMessage = $"Excluded collection contains {wrongExcluded.Count} change(s) with wrong type: {string.Join(", ", wrongExcluded.Select(c => $"'{c.Description}' ({c.Type})"))}";
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

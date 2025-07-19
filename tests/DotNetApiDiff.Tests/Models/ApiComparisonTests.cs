using DotNetApiDiff.Models;
using Xunit;

namespace DotNetApiDiff.Tests.Models;

public class ApiComparisonTests
{
    [Fact]
    public void ApiComparison_DefaultConstructor_InitializesCollections()
    {
        // Arrange & Act
        var comparison = new ApiComparison();

        // Assert
        Assert.NotNull(comparison.Additions);
        Assert.NotNull(comparison.Removals);
        Assert.NotNull(comparison.Modifications);
        Assert.NotNull(comparison.Excluded);
        Assert.NotNull(comparison.Summary);
        Assert.Empty(comparison.Additions);
        Assert.Empty(comparison.Removals);
        Assert.Empty(comparison.Modifications);
        Assert.Empty(comparison.Excluded);
    }

    [Fact]
    public void AllChanges_ReturnsAllChangesFromAllCollections()
    {
        // Arrange
        var comparison = new ApiComparison();
        var addition = new ApiChange { Type = ChangeType.Added, Description = "Added" };
        var removal = new ApiChange { Type = ChangeType.Removed, Description = "Removed" };
        var modification = new ApiChange { Type = ChangeType.Modified, Description = "Modified" };
        var excluded = new ApiChange { Type = ChangeType.Excluded, Description = "Excluded" };

        comparison.Additions.Add(addition);
        comparison.Removals.Add(removal);
        comparison.Modifications.Add(modification);
        comparison.Excluded.Add(excluded);

        // Act
        var allChanges = comparison.AllChanges.ToList();

        // Assert
        Assert.Equal(4, allChanges.Count);
        Assert.Contains(addition, allChanges);
        Assert.Contains(removal, allChanges);
        Assert.Contains(modification, allChanges);
        Assert.Contains(excluded, allChanges);
    }

    [Fact]
    public void HasBreakingChanges_WithBreakingChange_ReturnsTrue()
    {
        // Arrange
        var comparison = new ApiComparison();
        comparison.Removals.Add(new ApiChange
        {
            Type = ChangeType.Removed,
            Description = "Removed method",
            IsBreakingChange = true
        });

        // Act & Assert
        Assert.True(comparison.HasBreakingChanges);
    }

    [Fact]
    public void HasBreakingChanges_WithoutBreakingChanges_ReturnsFalse()
    {
        // Arrange
        var comparison = new ApiComparison();
        comparison.Additions.Add(new ApiChange
        {
            Type = ChangeType.Added,
            Description = "Added method",
            IsBreakingChange = false
        });

        // Act & Assert
        Assert.False(comparison.HasBreakingChanges);
    }

    [Fact]
    public void TotalChanges_ExcludesExcludedItems()
    {
        // Arrange
        var comparison = new ApiComparison();
        comparison.Additions.Add(new ApiChange { Type = ChangeType.Added, Description = "Added" });
        comparison.Removals.Add(new ApiChange { Type = ChangeType.Removed, Description = "Removed" });
        comparison.Modifications.Add(new ApiChange { Type = ChangeType.Modified, Description = "Modified" });
        comparison.Excluded.Add(new ApiChange { Type = ChangeType.Excluded, Description = "Excluded" });

        // Act
        var totalChanges = comparison.TotalChanges;

        // Assert
        Assert.Equal(3, totalChanges); // Should exclude the excluded item
    }

    [Fact]
    public void BreakingChangesCount_CountsAllBreakingChanges()
    {
        // Arrange
        var comparison = new ApiComparison();
        comparison.Additions.Add(new ApiChange
        {
            Type = ChangeType.Added,
            Description = "Added",
            IsBreakingChange = false
        });
        comparison.Removals.Add(new ApiChange
        {
            Type = ChangeType.Removed,
            Description = "Removed",
            IsBreakingChange = true
        });
        comparison.Modifications.Add(new ApiChange
        {
            Type = ChangeType.Modified,
            Description = "Modified",
            IsBreakingChange = true
        });

        // Act
        var breakingCount = comparison.BreakingChangesCount;

        // Assert
        Assert.Equal(2, breakingCount);
    }

    [Fact]
    public void IsValid_WithValidChanges_ReturnsTrue()
    {
        // Arrange
        var comparison = new ApiComparison();
        comparison.Additions.Add(new ApiChange
        {
            Type = ChangeType.Added,
            Description = "Added method",
            TargetMember = new ApiMember { Name = "Test", FullName = "Test.Method", Signature = "void Method()" }
        });
        comparison.Removals.Add(new ApiChange
        {
            Type = ChangeType.Removed,
            Description = "Removed method",
            SourceMember = new ApiMember { Name = "Test", FullName = "Test.Method", Signature = "void Method()" }
        });

        // Act & Assert
        Assert.True(comparison.IsValid());
    }

    [Fact]
    public void IsValid_WithInvalidChange_ReturnsFalse()
    {
        // Arrange
        var comparison = new ApiComparison();
        comparison.Additions.Add(new ApiChange
        {
            Type = ChangeType.Added,
            Description = "", // Invalid - empty description
            TargetMember = new ApiMember { Name = "Test", FullName = "Test.Method", Signature = "void Method()" }
        });

        // Act & Assert
        Assert.False(comparison.IsValid());
    }

    [Fact]
    public void IsValid_WithWrongChangeTypeInAdditions_ReturnsFalse()
    {
        // Arrange
        var comparison = new ApiComparison();
        comparison.Additions.Add(new ApiChange
        {
            Type = ChangeType.Removed, // Wrong type for Additions collection
            Description = "Should be added",
            TargetMember = new ApiMember { Name = "Test", FullName = "Test.Method", Signature = "void Method()" }
        });

        // Act & Assert
        Assert.False(comparison.IsValid());
    }

    [Fact]
    public void IsValid_WithWrongChangeTypeInRemovals_ReturnsFalse()
    {
        // Arrange
        var comparison = new ApiComparison();
        comparison.Removals.Add(new ApiChange
        {
            Type = ChangeType.Added, // Wrong type for Removals collection
            Description = "Should be removed",
            SourceMember = new ApiMember { Name = "Test", FullName = "Test.Method", Signature = "void Method()" }
        });

        // Act & Assert
        Assert.False(comparison.IsValid());
    }

    [Fact]
    public void IsValid_WithWrongChangeTypeInModifications_ReturnsFalse()
    {
        // Arrange
        var comparison = new ApiComparison();
        comparison.Modifications.Add(new ApiChange
        {
            Type = ChangeType.Added, // Wrong type for Modifications collection
            Description = "Should be modified",
            SourceMember = new ApiMember { Name = "Test", FullName = "Test.Method", Signature = "void Method()" },
            TargetMember = new ApiMember { Name = "Test", FullName = "Test.Method", Signature = "int Method()" }
        });

        // Act & Assert
        Assert.False(comparison.IsValid());
    }

    [Fact]
    public void IsValid_WithWrongChangeTypeInExcluded_ReturnsFalse()
    {
        // Arrange
        var comparison = new ApiComparison();
        comparison.Excluded.Add(new ApiChange
        {
            Type = ChangeType.Added, // Wrong type for Excluded collection
            Description = "Should be excluded"
        });

        // Act & Assert
        Assert.False(comparison.IsValid());
    }

    [Fact]
    public void UpdateSummary_UpdatesAllCounts()
    {
        // Arrange
        var comparison = new ApiComparison();
        comparison.Additions.Add(new ApiChange { Type = ChangeType.Added, Description = "Added 1", IsBreakingChange = false });
        comparison.Additions.Add(new ApiChange { Type = ChangeType.Added, Description = "Added 2", IsBreakingChange = true });
        comparison.Removals.Add(new ApiChange { Type = ChangeType.Removed, Description = "Removed 1", IsBreakingChange = true });
        comparison.Modifications.Add(new ApiChange { Type = ChangeType.Modified, Description = "Modified 1", IsBreakingChange = false });

        // Act
        comparison.UpdateSummary();

        // Assert
        Assert.Equal(2, comparison.Summary.AddedCount);
        Assert.Equal(1, comparison.Summary.RemovedCount);
        Assert.Equal(1, comparison.Summary.ModifiedCount);
        Assert.Equal(2, comparison.Summary.BreakingChangesCount);
    }
}

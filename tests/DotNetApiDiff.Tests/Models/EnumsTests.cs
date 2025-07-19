using DotNetApiDiff.Models;
using Xunit;

namespace DotNetApiDiff.Tests.Models;

public class EnumsTests
{
    [Fact]
    public void ChangeType_HasExpectedValues()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(ChangeType), ChangeType.Added));
        Assert.True(Enum.IsDefined(typeof(ChangeType), ChangeType.Removed));
        Assert.True(Enum.IsDefined(typeof(ChangeType), ChangeType.Modified));
        Assert.True(Enum.IsDefined(typeof(ChangeType), ChangeType.Moved));
        Assert.True(Enum.IsDefined(typeof(ChangeType), ChangeType.Excluded));
    }

    [Fact]
    public void MemberType_HasExpectedValues()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(MemberType), MemberType.Class));
        Assert.True(Enum.IsDefined(typeof(MemberType), MemberType.Interface));
        Assert.True(Enum.IsDefined(typeof(MemberType), MemberType.Struct));
        Assert.True(Enum.IsDefined(typeof(MemberType), MemberType.Enum));
        Assert.True(Enum.IsDefined(typeof(MemberType), MemberType.Delegate));
        Assert.True(Enum.IsDefined(typeof(MemberType), MemberType.Method));
        Assert.True(Enum.IsDefined(typeof(MemberType), MemberType.Property));
        Assert.True(Enum.IsDefined(typeof(MemberType), MemberType.Field));
        Assert.True(Enum.IsDefined(typeof(MemberType), MemberType.Event));
        Assert.True(Enum.IsDefined(typeof(MemberType), MemberType.Constructor));
    }

    [Fact]
    public void AccessibilityLevel_HasExpectedValues()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(AccessibilityLevel), AccessibilityLevel.Private));
        Assert.True(Enum.IsDefined(typeof(AccessibilityLevel), AccessibilityLevel.Protected));
        Assert.True(Enum.IsDefined(typeof(AccessibilityLevel), AccessibilityLevel.Internal));
        Assert.True(Enum.IsDefined(typeof(AccessibilityLevel), AccessibilityLevel.ProtectedInternal));
        Assert.True(Enum.IsDefined(typeof(AccessibilityLevel), AccessibilityLevel.ProtectedPrivate));
        Assert.True(Enum.IsDefined(typeof(AccessibilityLevel), AccessibilityLevel.Public));
    }

    [Fact]
    public void ApiElementType_HasExpectedValues()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(ApiElementType), ApiElementType.Assembly));
        Assert.True(Enum.IsDefined(typeof(ApiElementType), ApiElementType.Namespace));
        Assert.True(Enum.IsDefined(typeof(ApiElementType), ApiElementType.Type));
        Assert.True(Enum.IsDefined(typeof(ApiElementType), ApiElementType.Method));
        Assert.True(Enum.IsDefined(typeof(ApiElementType), ApiElementType.Property));
        Assert.True(Enum.IsDefined(typeof(ApiElementType), ApiElementType.Field));
        Assert.True(Enum.IsDefined(typeof(ApiElementType), ApiElementType.Event));
        Assert.True(Enum.IsDefined(typeof(ApiElementType), ApiElementType.Constructor));
    }

    [Fact]
    public void SeverityLevel_HasExpectedValues()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(SeverityLevel), SeverityLevel.Info));
        Assert.True(Enum.IsDefined(typeof(SeverityLevel), SeverityLevel.Warning));
        Assert.True(Enum.IsDefined(typeof(SeverityLevel), SeverityLevel.Error));
        Assert.True(Enum.IsDefined(typeof(SeverityLevel), SeverityLevel.Critical));
    }

    [Fact]
    public void ReportFormat_HasExpectedValues()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(ReportFormat), ReportFormat.Console));
        Assert.True(Enum.IsDefined(typeof(ReportFormat), ReportFormat.Json));
        Assert.True(Enum.IsDefined(typeof(ReportFormat), ReportFormat.Xml));
        Assert.True(Enum.IsDefined(typeof(ReportFormat), ReportFormat.Html));
        Assert.True(Enum.IsDefined(typeof(ReportFormat), ReportFormat.Markdown));
    }

    [Theory]
    [InlineData(ChangeType.Added)]
    [InlineData(ChangeType.Removed)]
    [InlineData(ChangeType.Modified)]
    [InlineData(ChangeType.Moved)]
    [InlineData(ChangeType.Excluded)]
    public void ChangeType_CanConvertToString(ChangeType changeType)
    {
        // Act
        var result = changeType.ToString();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Theory]
    [InlineData(MemberType.Class)]
    [InlineData(MemberType.Method)]
    [InlineData(MemberType.Property)]
    public void MemberType_CanConvertToString(MemberType memberType)
    {
        // Act
        var result = memberType.ToString();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Theory]
    [InlineData(AccessibilityLevel.Public)]
    [InlineData(AccessibilityLevel.Private)]
    [InlineData(AccessibilityLevel.Protected)]
    public void AccessibilityLevel_CanConvertToString(AccessibilityLevel accessibilityLevel)
    {
        // Act
        var result = accessibilityLevel.ToString();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }
}

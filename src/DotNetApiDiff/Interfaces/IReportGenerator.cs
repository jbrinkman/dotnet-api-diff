using DotNetApiDiff.Models;

namespace DotNetApiDiff.Interfaces;

/// <summary>
/// Interface for generating comparison reports in various formats
/// </summary>
public interface IReportGenerator
{
    /// <summary>
    /// Generates a report from the comparison result
    /// </summary>
    /// <param name="result">The comparison result to generate a report from</param>
    /// <param name="format">The desired output format</param>
    /// <returns>Generated report as a string</returns>
    string GenerateReport(ComparisonResult result, ReportFormat format);

    /// <summary>
    /// Saves a report to the specified file path
    /// </summary>
    /// <param name="result">The comparison result to generate a report from</param>
    /// <param name="format">The desired output format</param>
    /// <param name="filePath">Path where the report should be saved</param>
    Task SaveReportAsync(ComparisonResult result, ReportFormat format, string filePath);

    /// <summary>
    /// Gets the supported report formats
    /// </summary>
    /// <returns>Collection of supported report formats</returns>
    IEnumerable<ReportFormat> GetSupportedFormats();
}
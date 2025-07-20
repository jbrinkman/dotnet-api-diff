// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using DotNetApiDiff.Interfaces;
using DotNetApiDiff.Models;
using Microsoft.Extensions.Logging;

namespace DotNetApiDiff.Reporting;

/// <summary>
/// Implementation of the report generator that supports multiple output formats
/// </summary>
public class ReportGenerator : IReportGenerator
{
    private readonly ILogger<ReportGenerator> _logger;
    private readonly Dictionary<ReportFormat, IReportFormatter> _formatters;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportGenerator"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="formatters">Optional dictionary of formatters to use. If null, default formatters will be created.</param>
    public ReportGenerator(ILogger<ReportGenerator> logger, Dictionary<ReportFormat, IReportFormatter>? formatters = null)
    {
        _logger = logger;

        if (formatters != null)
        {
            _formatters = formatters;
        }
        else
        {
            _formatters = new Dictionary<ReportFormat, IReportFormatter>
            {
                { ReportFormat.Console, new ConsoleFormatter() }

                // Other formatters will be added in subsequent tasks
                // { ReportFormat.Json, new JsonFormatter() }
                // { ReportFormat.Markdown, new MarkdownFormatter() }
            };
        }
    }

    /// <summary>
    /// Generates a report from the comparison result
    /// </summary>
    /// <param name="result">The comparison result to generate a report from</param>
    /// <param name="format">The desired output format</param>
    /// <returns>Generated report as a string</returns>
    public string GenerateReport(ComparisonResult result, ReportFormat format)
    {
        _logger.LogInformation("Generating report in {Format} format", format);

        if (_formatters.TryGetValue(format, out var formatter))
        {
            return formatter.Format(result);
        }

        _logger.LogWarning("Requested format {Format} is not supported, falling back to Console format", format);
        return _formatters[ReportFormat.Console].Format(result);
    }

    /// <summary>
    /// Saves a report to the specified file path
    /// </summary>
    /// <param name="result">The comparison result to generate a report from</param>
    /// <param name="format">The desired output format</param>
    /// <param name="filePath">Path where the report should be saved</param>
    public async Task SaveReportAsync(ComparisonResult result, ReportFormat format, string filePath)
    {
        _logger.LogInformation("Saving {Format} report to {FilePath}", format, filePath);

        var report = GenerateReport(result, format);

        try
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(filePath, report);
            _logger.LogInformation("Report saved successfully to {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save report to {FilePath}", filePath);
            throw;
        }
    }

    /// <summary>
    /// Gets the supported report formats
    /// </summary>
    /// <returns>Collection of supported report formats</returns>
    public IEnumerable<ReportFormat> GetSupportedFormats()
    {
        return _formatters.Keys;
    }
}

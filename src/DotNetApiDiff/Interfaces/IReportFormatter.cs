// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using DotNetApiDiff.Models;

namespace DotNetApiDiff.Interfaces;

/// <summary>
/// Interface for formatting comparison results into specific output formats
/// </summary>
public interface IReportFormatter
{
    /// <summary>
    /// Formats a comparison result into a specific output format
    /// </summary>
    /// <param name="result">The comparison result to format</param>
    /// <returns>Formatted output as a string</returns>
    string Format(ComparisonResult result);
}

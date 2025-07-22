// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using DotNetApiDiff.Models;

namespace DotNetApiDiff.Interfaces
{
    /// <summary>
    /// Interface for managing application exit codes based on comparison results.
    /// </summary>
    public interface IExitCodeManager
    {
        /// <summary>
        /// Determines the appropriate exit code based on comparison results.
        /// </summary>
        /// <param name="hasBreakingChanges">Whether breaking changes were detected.</param>
        /// <param name="hasErrors">Whether errors occurred during comparison.</param>
        /// <returns>The appropriate exit code.</returns>
        int GetExitCode(bool hasBreakingChanges, bool hasErrors);

        /// <summary>
        /// Determines the appropriate exit code based on a comparison result.
        /// </summary>
        /// <param name="comparisonResult">The comparison result to evaluate.</param>
        /// <returns>The appropriate exit code.</returns>
        int GetExitCode(ComparisonResult comparisonResult);

        /// <summary>
        /// Determines the appropriate exit code based on an API comparison.
        /// </summary>
        /// <param name="apiComparison">The API comparison to evaluate.</param>
        /// <returns>The appropriate exit code.</returns>
        int GetExitCode(ApiComparison apiComparison);

        /// <summary>
        /// Determines the appropriate exit code for an exception scenario.
        /// </summary>
        /// <param name="exception">The exception that occurred.</param>
        /// <returns>The appropriate exit code.</returns>
        int GetExitCodeForException(Exception exception);

        /// <summary>
        /// Determines the appropriate exit code based on a combination of API comparison and exception.
        /// </summary>
        /// <param name="apiComparison">The API comparison to evaluate, or null if not available.</param>
        /// <param name="exception">The exception that occurred, or null if no exception.</param>
        /// <returns>The appropriate exit code.</returns>
        int GetExitCode(ApiComparison? apiComparison, Exception? exception);

        /// <summary>
        /// Gets a human-readable description of the exit code.
        /// </summary>
        /// <param name="exitCode">The exit code to describe.</param>
        /// <returns>A description of what the exit code means.</returns>
        string GetExitCodeDescription(int exitCode);
    }
}

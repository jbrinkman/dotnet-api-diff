// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
namespace DotNetApiDiff.Interfaces
{
    /// <summary>
    /// Interface for global exception handling.
    /// </summary>
    public interface IGlobalExceptionHandler
    {
        /// <summary>
        /// Handles an exception by logging it and determining the appropriate exit code.
        /// </summary>
        /// <param name="exception">The exception to handle.</param>
        /// <param name="context">Optional context information about where the exception occurred.</param>
        /// <returns>The appropriate exit code for the exception.</returns>
        int HandleException(Exception exception, string? context = null);

        /// <summary>
        /// Sets up global unhandled exception handling.
        /// </summary>
        void SetupGlobalExceptionHandling();
    }
}

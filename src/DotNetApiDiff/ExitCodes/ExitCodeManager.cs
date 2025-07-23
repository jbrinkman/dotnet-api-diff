// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using System.IO;
using System.Reflection;
using System.Security;
using DotNetApiDiff.Interfaces;
using DotNetApiDiff.Models;
using Microsoft.Extensions.Logging;

namespace DotNetApiDiff.ExitCodes
{
    /// <summary>
    /// Manages application exit codes based on comparison results.
    /// </summary>
    public class ExitCodeManager : IExitCodeManager
    {
        private readonly ILogger<ExitCodeManager>? _logger;

        /// <summary>
        /// Exit code for successful comparison with no breaking changes.
        /// </summary>
        public const int Success = 0;

        /// <summary>
        /// Exit code for successful comparison with breaking changes detected.
        /// </summary>
        public const int BreakingChangesDetected = 1;

        /// <summary>
        /// Exit code for errors during comparison.
        /// </summary>
        public const int ComparisonError = 2;

        /// <summary>
        /// Exit code for assembly loading failures.
        /// </summary>
        public const int AssemblyLoadError = 3;

        /// <summary>
        /// Exit code for configuration errors.
        /// </summary>
        public const int ConfigurationError = 4;

        /// <summary>
        /// Exit code for invalid command line arguments.
        /// </summary>
        public const int InvalidArguments = 5;

        /// <summary>
        /// Exit code for file not found errors.
        /// </summary>
        public const int FileNotFound = 6;

        /// <summary>
        /// Exit code for unexpected/unhandled errors.
        /// </summary>
        public const int UnexpectedError = 99;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExitCodeManager"/> class.
        /// </summary>
        public ExitCodeManager()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExitCodeManager"/> class with a logger.
        /// </summary>
        /// <param name="logger">The logger to use for logging exit code information.</param>
        public ExitCodeManager(ILogger<ExitCodeManager> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Determines the appropriate exit code based on comparison results.
        /// </summary>
        /// <param name="hasBreakingChanges">Whether breaking changes were detected.</param>
        /// <param name="hasErrors">Whether errors occurred during comparison.</param>
        /// <returns>The appropriate exit code.</returns>
        public int GetExitCode(bool hasBreakingChanges, bool hasErrors)
        {
            if (hasErrors)
            {
                _logger?.LogWarning("Errors occurred during comparison, returning ComparisonError exit code");
                return ComparisonError;
            }

            if (hasBreakingChanges)
            {
                _logger?.LogWarning("Breaking changes detected, returning BreakingChangesDetected exit code");
                return BreakingChangesDetected;
            }

            _logger?.LogInformation("No breaking changes or errors detected, returning Success exit code");
            return Success;
        }

        /// <summary>
        /// Determines the appropriate exit code based on a comparison result.
        /// </summary>
        /// <param name="comparisonResult">The comparison result to evaluate.</param>
        /// <returns>The appropriate exit code.</returns>
        public int GetExitCode(ComparisonResult comparisonResult)
        {
            if (comparisonResult == null)
            {
                _logger?.LogWarning("Comparison result is null, returning ComparisonError exit code");
                return ComparisonError;
            }

            // Check for breaking changes
            if (comparisonResult.HasBreakingChanges)
            {
                _logger?.LogWarning("Breaking changes detected in comparison result, returning BreakingChangesDetected exit code");
                return BreakingChangesDetected;
            }

            _logger?.LogInformation("No breaking changes detected in comparison result, returning Success exit code");
            return Success;
        }

        /// <summary>
        /// Determines the appropriate exit code based on an API comparison.
        /// </summary>
        /// <param name="apiComparison">The API comparison to evaluate.</param>
        /// <returns>The appropriate exit code.</returns>
        public int GetExitCode(ApiComparison apiComparison)
        {
            if (apiComparison == null)
            {
                _logger?.LogWarning("API comparison is null, returning ComparisonError exit code");
                return ComparisonError;
            }

            // Validate the comparison result
            if (!apiComparison.IsValid())
            {
                _logger?.LogWarning("API comparison is invalid, returning ComparisonError exit code");
                return ComparisonError;
            }

            // Check for breaking changes
            if (apiComparison.HasBreakingChanges)
            {
                int breakingChangesCount = apiComparison.BreakingChangesCount;
                _logger?.LogWarning("{Count} breaking changes detected in API comparison, returning BreakingChangesDetected exit code", breakingChangesCount);
                return BreakingChangesDetected;
            }

            _logger?.LogInformation("No breaking changes detected in API comparison, returning Success exit code");
            return Success;
        }

        /// <summary>
        /// Determines the appropriate exit code for an exception scenario.
        /// </summary>
        /// <param name="exception">The exception that occurred.</param>
        /// <returns>The appropriate exit code.</returns>
        public int GetExitCodeForException(Exception exception)
        {
            if (exception == null)
            {
                _logger?.LogWarning("Exception is null, returning UnexpectedError exit code");
                return UnexpectedError;
            }

            int exitCode = exception switch
            {
                FileNotFoundException => FileNotFound,
                DirectoryNotFoundException => FileNotFound,
                ReflectionTypeLoadException => AssemblyLoadError,
                BadImageFormatException => AssemblyLoadError,
                SecurityException => AssemblyLoadError,
                ArgumentNullException => InvalidArguments,
                ArgumentException => InvalidArguments,
                InvalidOperationException => ConfigurationError,
                NotSupportedException => ConfigurationError,
                _ => UnexpectedError
            };

            _logger?.LogWarning(
                "Exception of type {ExceptionType} occurred, returning {ExitCode} exit code",
                exception.GetType().Name,
                GetExitCodeDescription(exitCode));

            return exitCode;
        }

        /// <summary>
        /// Determines the appropriate exit code based on a combination of API comparison and exception.
        /// </summary>
        /// <param name="apiComparison">The API comparison to evaluate, or null if not available.</param>
        /// <param name="exception">The exception that occurred, or null if no exception.</param>
        /// <returns>The appropriate exit code.</returns>
        public int GetExitCode(ApiComparison? apiComparison, Exception? exception)
        {
            // If there's an exception, it takes precedence
            if (exception != null)
            {
                _logger?.LogWarning("Exception present, determining exit code based on exception");
                return GetExitCodeForException(exception);
            }

            // If there's no API comparison, return comparison error
            if (apiComparison == null)
            {
                _logger?.LogWarning("API comparison is null, returning ComparisonError exit code");
                return ComparisonError;
            }

            // Otherwise, determine based on the API comparison
            _logger?.LogInformation("Determining exit code based on API comparison");
            return GetExitCode(apiComparison);
        }

        /// <summary>
        /// Gets a human-readable description of the exit code.
        /// </summary>
        /// <param name="exitCode">The exit code to describe.</param>
        /// <returns>A description of what the exit code means.</returns>
        public string GetExitCodeDescription(int exitCode)
        {
            return exitCode switch
            {
                Success => "Comparison completed successfully with no breaking changes detected.",
                BreakingChangesDetected => "Comparison completed successfully but breaking changes were detected.",
                ComparisonError => "An error occurred during the comparison process.",
                AssemblyLoadError => "Failed to load one or more assemblies for comparison.",
                ConfigurationError => "Configuration error or invalid settings detected.",
                InvalidArguments => "Invalid command line arguments provided.",
                FileNotFound => "One or more required files could not be found.",
                UnexpectedError => "An unexpected error occurred during execution.",
                _ => $"Unknown exit code: {exitCode}"
            };
        }
    }
}

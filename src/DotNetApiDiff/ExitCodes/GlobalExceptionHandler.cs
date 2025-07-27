// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using System.Reflection;
using System.Security;
using DotNetApiDiff.Interfaces;
using Microsoft.Extensions.Logging;

namespace DotNetApiDiff.ExitCodes
{
    /// <summary>
    /// Provides centralized exception handling for the application.
    /// </summary>
    public class GlobalExceptionHandler : IGlobalExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IExitCodeManager _exitCodeManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalExceptionHandler"/> class.
        /// </summary>
        /// <param name="logger">The logger to use for logging exceptions.</param>
        /// <param name="exitCodeManager">The exit code manager to determine appropriate exit codes.</param>
        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IExitCodeManager exitCodeManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exitCodeManager = exitCodeManager ?? throw new ArgumentNullException(nameof(exitCodeManager));
        }

        /// <summary>
        /// Handles an exception by logging it and determining the appropriate exit code.
        /// </summary>
        /// <param name="exception">The exception to handle.</param>
        /// <param name="context">Optional context information about where the exception occurred.</param>
        /// <returns>The appropriate exit code for the exception.</returns>
        public int HandleException(Exception exception, string? context = null)
        {
            if (exception == null)
            {
                _logger.LogError("HandleException called with null exception");
                return _exitCodeManager.GetExitCodeForException(new ArgumentNullException(nameof(exception)));
            }

            // Log the exception with context if provided
            if (!string.IsNullOrEmpty(context))
            {
                _logger.LogError(exception, "Error in {Context}: {Message}", context, exception.Message);
            }
            else
            {
                _logger.LogError(exception, "Error: {Message}", exception.Message);
            }

            // Log additional details for specific exception types
            LogExceptionDetails(exception);

            // Determine the appropriate exit code
            int exitCode = _exitCodeManager.GetExitCodeForException(exception);

            _logger.LogInformation(
                "Exiting with code {ExitCode}: {Description}",
                exitCode,
                _exitCodeManager.GetExitCodeDescription(exitCode));

            return exitCode;
        }

        /// <summary>
        /// Logs additional details for specific exception types.
        /// </summary>
        /// <param name="exception">The exception to log details for.</param>
        private void LogExceptionDetails(Exception exception)
        {
            switch (exception)
            {
                case ReflectionTypeLoadException typeLoadEx:
                    LogReflectionTypeLoadException(typeLoadEx);
                    break;
                case AggregateException aggregateEx:
                    LogAggregateException(aggregateEx);
                    break;
                case FileNotFoundException fileNotFoundEx:
                    _logger.LogError("File not found: {FileName}", fileNotFoundEx.FileName);
                    break;
                case BadImageFormatException badImageEx:
                    _logger.LogError("Bad image format: {FileName}", badImageEx.FileName);
                    break;
                case SecurityException securityEx:
                    _logger.LogError("Security exception: {PermissionType}", securityEx.PermissionType);
                    break;
                case InvalidOperationException:
                    // Log the stack trace for InvalidOperationException to help diagnose the issue
                    _logger.LogDebug("Stack trace: {StackTrace}", exception.StackTrace);
                    break;
            }

            // Log inner exception if present
            if (exception.InnerException != null)
            {
                _logger.LogDebug("Inner exception: {Message}", exception.InnerException.Message);
            }
        }

        /// <summary>
        /// Logs details for a ReflectionTypeLoadException.
        /// </summary>
        /// <param name="exception">The ReflectionTypeLoadException to log details for.</param>
        private void LogReflectionTypeLoadException(ReflectionTypeLoadException exception)
        {
            _logger.LogError("ReflectionTypeLoadException: Failed to load {Count} types", exception.Types?.Length ?? 0);

            if (exception.LoaderExceptions != null)
            {
                int loaderExceptionCount = exception.LoaderExceptions.Length;
                _logger.LogError("Loader exceptions count: {Count}", loaderExceptionCount);

                // Log up to 5 loader exceptions to avoid excessive logging
                int logCount = Math.Min(loaderExceptionCount, 5);
                for (int i = 0; i < logCount; i++)
                {
                    var loaderEx = exception.LoaderExceptions[i];
                    if (loaderEx != null)
                    {
                        _logger.LogError(loaderEx, "Loader exception {Index}: {Message}", i + 1, loaderEx.Message);
                    }
                }

                if (loaderExceptionCount > logCount)
                {
                    _logger.LogError("... and {Count} more loader exceptions", loaderExceptionCount - logCount);
                }
            }
        }

        /// <summary>
        /// Logs details for an AggregateException.
        /// </summary>
        /// <param name="exception">The AggregateException to log details for.</param>
        private void LogAggregateException(AggregateException exception)
        {
            _logger.LogError("AggregateException with {Count} inner exceptions", exception.InnerExceptions.Count);

            // Log up to 5 inner exceptions to avoid excessive logging
            int logCount = Math.Min(exception.InnerExceptions.Count, 5);
            for (int i = 0; i < logCount; i++)
            {
                var innerEx = exception.InnerExceptions[i];
                _logger.LogError(innerEx, "Inner exception {Index}: {Message}", i + 1, innerEx.Message);
            }

            if (exception.InnerExceptions.Count > logCount)
            {
                _logger.LogError("... and {Count} more inner exceptions", exception.InnerExceptions.Count - logCount);
            }
        }

        /// <summary>
        /// Sets up global unhandled exception handling.
        /// </summary>
        public void SetupGlobalExceptionHandling()
        {
            // Handle unhandled exceptions in the current AppDomain
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    _logger.LogCritical(ex, "Unhandled exception in AppDomain: {Message}", ex.Message);
                }
                else
                {
                    _logger.LogCritical("Unhandled non-exception object in AppDomain: {Object}", e.ExceptionObject);
                }
            };

            // Handle unhandled exceptions in tasks
            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                _logger.LogCritical(e.Exception, "Unobserved task exception: {Message}", e.Exception.Message);
                e.SetObserved(); // Mark as observed to prevent process termination
            };

            // Handle first-chance exceptions (useful for debugging)
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                // Register for FirstChanceException events only when debug logging is enabled
                AppDomain.CurrentDomain.FirstChanceException += (sender, e) =>
                {
                    // Only log first-chance exceptions at debug level to avoid noise
                    _logger.LogDebug(e.Exception, "First chance exception: {Message}", e.Exception.Message);
                };
            }
        }
    }
}

// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using System;
using System.IO;
using System.Reflection;
using System.Security;
using DotNetApiDiff.ExitCodes;
using DotNetApiDiff.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotNetApiDiff.Tests.ExitCodes
{
    public class ExitCodeManagerTests
    {
        private readonly ExitCodeManager _exitCodeManager;
        private readonly ExitCodeManager _exitCodeManagerWithLogger;
        private readonly Mock<ILogger<ExitCodeManager>> _loggerMock;

        public ExitCodeManagerTests()
        {
            _exitCodeManager = new ExitCodeManager();
            _loggerMock = new Mock<ILogger<ExitCodeManager>>();
            _exitCodeManagerWithLogger = new ExitCodeManager(_loggerMock.Object);
        }

        #region Basic GetExitCode Tests

        [Fact]
        public void GetExitCode_NoBreakingChangesNoErrors_ReturnsSuccess()
        {
            // Act
            int exitCode = _exitCodeManager.GetExitCode(hasBreakingChanges: false, hasErrors: false);

            // Assert
            Assert.Equal(ExitCodeManager.Success, exitCode);
        }

        [Fact]
        public void GetExitCode_WithBreakingChangesNoErrors_ReturnsBreakingChangesDetected()
        {
            // Act
            int exitCode = _exitCodeManager.GetExitCode(hasBreakingChanges: true, hasErrors: false);

            // Assert
            Assert.Equal(ExitCodeManager.BreakingChangesDetected, exitCode);
        }

        [Fact]
        public void GetExitCode_WithErrors_ReturnsComparisonError()
        {
            // Act
            int exitCode = _exitCodeManager.GetExitCode(hasBreakingChanges: false, hasErrors: true);

            // Assert
            Assert.Equal(ExitCodeManager.ComparisonError, exitCode);
        }

        [Fact]
        public void GetExitCode_WithBreakingChangesAndErrors_ReturnsComparisonError()
        {
            // Act
            int exitCode = _exitCodeManager.GetExitCode(hasBreakingChanges: true, hasErrors: true);

            // Assert
            Assert.Equal(ExitCodeManager.ComparisonError, exitCode);
        }

        [Fact]
        public void GetExitCode_WithLogger_LogsAppropriateMessages()
        {
            // Act
            _exitCodeManagerWithLogger.GetExitCode(hasBreakingChanges: true, hasErrors: false);

            // Assert - Verify that the logger was called with the expected message
            // Note: We can't easily verify the exact log message without making the test brittle,
            // but we can verify that logging occurred
            _loggerMock.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        #endregion

        #region ComparisonResult Tests

        [Fact]
        public void GetExitCode_ComparisonResultWithNoBreakingChanges_ReturnsSuccess()
        {
            // Arrange
            var comparisonResult = new ComparisonResult
            {
                OldAssemblyPath = "old.dll",
                NewAssemblyPath = "new.dll",
                Differences = new List<ApiDifference>
                {
                    new ApiDifference { IsBreakingChange = false }
                }
            };

            // Act
            int exitCode = _exitCodeManager.GetExitCode(comparisonResult);

            // Assert
            Assert.Equal(ExitCodeManager.Success, exitCode);
        }

        [Fact]
        public void GetExitCode_ComparisonResultWithBreakingChanges_ReturnsBreakingChangesDetected()
        {
            // Arrange
            var comparisonResult = new ComparisonResult
            {
                OldAssemblyPath = "old.dll",
                NewAssemblyPath = "new.dll",
                Differences = new List<ApiDifference>
                {
                    new ApiDifference { IsBreakingChange = true }
                }
            };

            // Act
            int exitCode = _exitCodeManager.GetExitCode(comparisonResult);

            // Assert
            Assert.Equal(ExitCodeManager.BreakingChangesDetected, exitCode);
        }

        [Fact]
        public void GetExitCode_NullComparisonResult_ReturnsComparisonError()
        {
            // Act
            ComparisonResult comparisonResult = null;
            int exitCode = _exitCodeManager.GetExitCode(comparisonResult);

            // Assert
            Assert.Equal(ExitCodeManager.ComparisonError, exitCode);
        }

        [Fact]
        public void GetExitCode_EmptyComparisonResult_ReturnsSuccess()
        {
            // Arrange
            var comparisonResult = new ComparisonResult
            {
                OldAssemblyPath = "old.dll",
                NewAssemblyPath = "new.dll",
                Differences = new List<ApiDifference>()
            };

            // Act
            int exitCode = _exitCodeManager.GetExitCode(comparisonResult);

            // Assert
            Assert.Equal(ExitCodeManager.Success, exitCode);
        }

        #endregion

        #region ApiComparison Tests

        [Fact]
        public void GetExitCode_ApiComparisonWithNoBreakingChanges_ReturnsSuccess()
        {
            // Arrange
            var apiComparison = new ApiComparison
            {
                Additions = new List<ApiChange>
                {
                    new ApiChange
                    {
                        Type = ChangeType.Added,
                        Description = "Added method",
                        IsBreakingChange = false,
                        TargetMember = new ApiMember { FullName = "Test.Method" }
                    }
                }
            };

            // Act
            int exitCode = _exitCodeManager.GetExitCode(apiComparison);

            // Assert
            Assert.Equal(ExitCodeManager.Success, exitCode);
        }

        [Fact]
        public void GetExitCode_ApiComparisonWithBreakingChanges_ReturnsBreakingChangesDetected()
        {
            // Arrange
            var apiComparison = new ApiComparison
            {
                Removals = new List<ApiChange>
                {
                    new ApiChange
                    {
                        Type = ChangeType.Removed,
                        Description = "Removed method",
                        IsBreakingChange = true,
                        SourceMember = new ApiMember { FullName = "Test.Method" }
                    }
                }
            };

            // Act
            int exitCode = _exitCodeManager.GetExitCode(apiComparison);

            // Assert
            Assert.Equal(ExitCodeManager.BreakingChangesDetected, exitCode);
        }

        [Fact]
        public void GetExitCode_NullApiComparison_ReturnsComparisonError()
        {
            // Act
            int exitCode = _exitCodeManager.GetExitCode((ApiComparison)null);

            // Assert
            Assert.Equal(ExitCodeManager.ComparisonError, exitCode);
        }

        [Fact]
        public void GetExitCode_InvalidApiComparison_ReturnsComparisonError()
        {
            // Arrange - Create an invalid ApiComparison (Added change without target member)
            var apiComparison = new ApiComparison
            {
                Additions = new List<ApiChange>
                {
                    new ApiChange
                    {
                        Type = ChangeType.Added,
                        Description = "Added method",
                        TargetMember = null // This makes it invalid
                    }
                }
            };

            // Act
            int exitCode = _exitCodeManager.GetExitCode(apiComparison);

            // Assert
            Assert.Equal(ExitCodeManager.ComparisonError, exitCode);
        }

        [Fact]
        public void GetExitCode_EmptyApiComparison_ReturnsSuccess()
        {
            // Arrange
            var apiComparison = new ApiComparison();

            // Act
            int exitCode = _exitCodeManager.GetExitCode(apiComparison);

            // Assert
            Assert.Equal(ExitCodeManager.Success, exitCode);
        }

        [Fact]
        public void GetExitCode_ApiComparisonWithMixedChanges_ReturnsBreakingChangesDetected()
        {
            // Arrange
            var apiComparison = new ApiComparison
            {
                Additions = new List<ApiChange>
                {
                    new ApiChange
                    {
                        Type = ChangeType.Added,
                        Description = "Added method",
                        IsBreakingChange = false,
                        TargetMember = new ApiMember { FullName = "Test.Method1" }
                    }
                },
                Removals = new List<ApiChange>
                {
                    new ApiChange
                    {
                        Type = ChangeType.Removed,
                        Description = "Removed method",
                        IsBreakingChange = true,
                        SourceMember = new ApiMember { FullName = "Test.Method2" }
                    }
                }
            };

            // Act
            int exitCode = _exitCodeManager.GetExitCode(apiComparison);

            // Assert
            Assert.Equal(ExitCodeManager.BreakingChangesDetected, exitCode);
        }

        #endregion

        #region Exception Tests

        [Fact]
        public void GetExitCodeForException_FileNotFoundException_ReturnsFileNotFound()
        {
            // Arrange
            var exception = new FileNotFoundException("Assembly not found");

            // Act
            int exitCode = _exitCodeManager.GetExitCodeForException(exception);

            // Assert
            Assert.Equal(ExitCodeManager.FileNotFound, exitCode);
        }

        [Fact]
        public void GetExitCodeForException_DirectoryNotFoundException_ReturnsFileNotFound()
        {
            // Arrange
            var exception = new DirectoryNotFoundException("Directory not found");

            // Act
            int exitCode = _exitCodeManager.GetExitCodeForException(exception);

            // Assert
            Assert.Equal(ExitCodeManager.FileNotFound, exitCode);
        }

        [Fact]
        public void GetExitCodeForException_ReflectionTypeLoadException_ReturnsAssemblyLoadError()
        {
            // Arrange
            var exception = new ReflectionTypeLoadException(Array.Empty<Type>(), Array.Empty<Exception>());

            // Act
            int exitCode = _exitCodeManager.GetExitCodeForException(exception);

            // Assert
            Assert.Equal(ExitCodeManager.AssemblyLoadError, exitCode);
        }

        [Fact]
        public void GetExitCodeForException_BadImageFormatException_ReturnsAssemblyLoadError()
        {
            // Arrange
            var exception = new BadImageFormatException("Invalid assembly format");

            // Act
            int exitCode = _exitCodeManager.GetExitCodeForException(exception);

            // Assert
            Assert.Equal(ExitCodeManager.AssemblyLoadError, exitCode);
        }

        [Fact]
        public void GetExitCodeForException_SecurityException_ReturnsAssemblyLoadError()
        {
            // Arrange
            var exception = new SecurityException("Security error loading assembly");

            // Act
            int exitCode = _exitCodeManager.GetExitCodeForException(exception);

            // Assert
            Assert.Equal(ExitCodeManager.AssemblyLoadError, exitCode);
        }

        [Fact]
        public void GetExitCodeForException_ArgumentException_ReturnsInvalidArguments()
        {
            // Arrange
            var exception = new ArgumentException("Invalid argument");

            // Act
            int exitCode = _exitCodeManager.GetExitCodeForException(exception);

            // Assert
            Assert.Equal(ExitCodeManager.InvalidArguments, exitCode);
        }

        [Fact]
        public void GetExitCodeForException_ArgumentNullException_ReturnsInvalidArguments()
        {
            // Arrange
            var exception = new ArgumentNullException("parameter");

            // Act
            int exitCode = _exitCodeManager.GetExitCodeForException(exception);

            // Assert
            Assert.Equal(ExitCodeManager.InvalidArguments, exitCode);
        }

        [Fact]
        public void GetExitCodeForException_InvalidOperationException_ReturnsConfigurationError()
        {
            // Arrange
            var exception = new InvalidOperationException("Invalid operation");

            // Act
            int exitCode = _exitCodeManager.GetExitCodeForException(exception);

            // Assert
            Assert.Equal(ExitCodeManager.ConfigurationError, exitCode);
        }

        [Fact]
        public void GetExitCodeForException_NotSupportedException_ReturnsConfigurationError()
        {
            // Arrange
            var exception = new NotSupportedException("Operation not supported");

            // Act
            int exitCode = _exitCodeManager.GetExitCodeForException(exception);

            // Assert
            Assert.Equal(ExitCodeManager.ConfigurationError, exitCode);
        }

        [Fact]
        public void GetExitCodeForException_UnknownException_ReturnsUnexpectedError()
        {
            // Arrange
            var exception = new InvalidCastException("Unexpected error");

            // Act
            int exitCode = _exitCodeManager.GetExitCodeForException(exception);

            // Assert
            Assert.Equal(ExitCodeManager.UnexpectedError, exitCode);
        }

        [Fact]
        public void GetExitCodeForException_NullException_ReturnsUnexpectedError()
        {
            // Act
            int exitCode = _exitCodeManager.GetExitCodeForException(null);

            // Assert
            Assert.Equal(ExitCodeManager.UnexpectedError, exitCode);
        }

        #endregion

        #region Combined ApiComparison and Exception Tests

        [Fact]
        public void GetExitCode_WithExceptionAndApiComparison_PrioritizesException()
        {
            // Arrange
            var exception = new FileNotFoundException("Assembly not found");
            var apiComparison = new ApiComparison
            {
                Additions = new List<ApiChange>
                {
                    new ApiChange
                    {
                        Type = ChangeType.Added,
                        Description = "Added method",
                        IsBreakingChange = false,
                        TargetMember = new ApiMember { FullName = "Test.Method" }
                    }
                }
            };

            // Act
            int exitCode = _exitCodeManager.GetExitCode(apiComparison, exception);

            // Assert
            Assert.Equal(ExitCodeManager.FileNotFound, exitCode);
        }

        [Fact]
        public void GetExitCode_WithNullExceptionAndValidApiComparison_UsesApiComparison()
        {
            // Arrange
            var apiComparison = new ApiComparison
            {
                Removals = new List<ApiChange>
                {
                    new ApiChange
                    {
                        Type = ChangeType.Removed,
                        Description = "Removed method",
                        IsBreakingChange = true,
                        SourceMember = new ApiMember { FullName = "Test.Method" }
                    }
                }
            };

            // Act
            int exitCode = _exitCodeManager.GetExitCode(apiComparison, null);

            // Assert
            Assert.Equal(ExitCodeManager.BreakingChangesDetected, exitCode);
        }

        [Fact]
        public void GetExitCode_WithNullExceptionAndNullApiComparison_ReturnsComparisonError()
        {
            // Act
            int exitCode = _exitCodeManager.GetExitCode(null, null);

            // Assert
            Assert.Equal(ExitCodeManager.ComparisonError, exitCode);
        }

        [Fact]
        public void GetExitCode_WithExceptionAndNullApiComparison_UsesException()
        {
            // Arrange
            var exception = new ArgumentException("Invalid argument");

            // Act
            int exitCode = _exitCodeManager.GetExitCode(null, exception);

            // Assert
            Assert.Equal(ExitCodeManager.InvalidArguments, exitCode);
        }

        #endregion

        #region Exit Code Description Tests

        [Theory]
        [InlineData(ExitCodeManager.Success, "Comparison completed successfully with no breaking changes detected.")]
        [InlineData(ExitCodeManager.BreakingChangesDetected, "Comparison completed successfully but breaking changes were detected.")]
        [InlineData(ExitCodeManager.ComparisonError, "An error occurred during the comparison process.")]
        [InlineData(ExitCodeManager.AssemblyLoadError, "Failed to load one or more assemblies for comparison.")]
        [InlineData(ExitCodeManager.ConfigurationError, "Configuration error or invalid settings detected.")]
        [InlineData(ExitCodeManager.InvalidArguments, "Invalid command line arguments provided.")]
        [InlineData(ExitCodeManager.FileNotFound, "One or more required files could not be found.")]
        [InlineData(ExitCodeManager.UnexpectedError, "An unexpected error occurred during execution.")]
        public void GetExitCodeDescription_KnownExitCodes_ReturnsCorrectDescription(int exitCode, string expectedDescription)
        {
            // Act
            string description = _exitCodeManager.GetExitCodeDescription(exitCode);

            // Assert
            Assert.Equal(expectedDescription, description);
        }

        [Fact]
        public void GetExitCodeDescription_UnknownExitCode_ReturnsUnknownMessage()
        {
            // Arrange
            int unknownExitCode = 999;

            // Act
            string description = _exitCodeManager.GetExitCodeDescription(unknownExitCode);

            // Assert
            Assert.Equal($"Unknown exit code: {unknownExitCode}", description);
        }

        #endregion

        #region Exit Code Constants Tests

        [Fact]
        public void ExitCodeConstants_HaveExpectedValues()
        {
            // Assert
            Assert.Equal(0, ExitCodeManager.Success);
            Assert.Equal(1, ExitCodeManager.BreakingChangesDetected);
            Assert.Equal(2, ExitCodeManager.ComparisonError);
            Assert.Equal(3, ExitCodeManager.AssemblyLoadError);
            Assert.Equal(4, ExitCodeManager.ConfigurationError);
            Assert.Equal(5, ExitCodeManager.InvalidArguments);
            Assert.Equal(6, ExitCodeManager.FileNotFound);
            Assert.Equal(99, ExitCodeManager.UnexpectedError);
        }

        #endregion
    }
}

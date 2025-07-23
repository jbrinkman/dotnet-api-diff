// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using System;
using System.IO;
using System.Reflection;
using DotNetApiDiff.ExitCodes;
using DotNetApiDiff.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotNetApiDiff.Tests.ExitCodes
{
    public class GlobalExceptionHandlerTests
    {
        private readonly Mock<ILogger<GlobalExceptionHandler>> _loggerMock;
        private readonly Mock<IExitCodeManager> _exitCodeManagerMock;
        private readonly GlobalExceptionHandler _handler;

        public GlobalExceptionHandlerTests()
        {
            _loggerMock = new Mock<ILogger<GlobalExceptionHandler>>();
            _exitCodeManagerMock = new Mock<IExitCodeManager>();
            _handler = new GlobalExceptionHandler(_loggerMock.Object, _exitCodeManagerMock.Object);
        }

        [Fact]
        public void HandleException_WithNullException_LogsErrorAndReturnsExitCode()
        {
            // Arrange
            _exitCodeManagerMock.Setup(m => m.GetExitCodeForException(It.IsAny<ArgumentNullException>()))
                .Returns(99);

            // Act
            int result = _handler.HandleException(null);

            // Assert
            Assert.Equal(99, result);
            VerifyLoggerCalled(LogLevel.Error, "HandleException called with null exception");
        }

        [Fact]
        public void HandleException_WithException_LogsErrorAndReturnsExitCode()
        {
            // Arrange
            var exception = new InvalidOperationException("Test exception");
            _exitCodeManagerMock.Setup(m => m.GetExitCodeForException(exception))
                .Returns(42);
            _exitCodeManagerMock.Setup(m => m.GetExitCodeDescription(42))
                .Returns("Test description");

            // Act
            int result = _handler.HandleException(exception, "Test context");

            // Assert
            Assert.Equal(42, result);
            VerifyLoggerCalled(LogLevel.Error, "Error in Test context: Test exception");
            VerifyLoggerCalled(LogLevel.Information, "Exiting with code 42: Test description");
        }

        [Fact]
        public void HandleException_WithFileNotFoundException_LogsAdditionalDetails()
        {
            // Arrange
            var exception = new FileNotFoundException("File not found", "test.dll");
            _exitCodeManagerMock.Setup(m => m.GetExitCodeForException(exception))
                .Returns(6);
            _exitCodeManagerMock.Setup(m => m.GetExitCodeDescription(6))
                .Returns("File not found");

            // Act
            int result = _handler.HandleException(exception);

            // Assert
            Assert.Equal(6, result);
            VerifyLoggerCalled(LogLevel.Error, "Error: File not found");
            VerifyLoggerCalled(LogLevel.Error, "File not found: test.dll");
        }

        [Fact]
        public void HandleException_WithReflectionTypeLoadException_LogsLoaderExceptions()
        {
            // Arrange
            var innerExceptions = new Exception[]
            {
                new DllNotFoundException("Test DLL not found"),
                new TypeLoadException("Test type not found")
            };
            var exception = new ReflectionTypeLoadException(new Type[0], innerExceptions);
            _exitCodeManagerMock.Setup(m => m.GetExitCodeForException(exception))
                .Returns(3);
            _exitCodeManagerMock.Setup(m => m.GetExitCodeDescription(3))
                .Returns("Assembly load error");

            // Act
            int result = _handler.HandleException(exception);

            // Assert
            Assert.Equal(3, result);
            VerifyLoggerCalled(LogLevel.Error, "ReflectionTypeLoadException: Failed to load 0 types");
            VerifyLoggerCalled(LogLevel.Error, "Loader exceptions count: 2");
        }

        [Fact]
        public void HandleException_WithAggregateException_LogsInnerExceptions()
        {
            // Arrange
            var innerExceptions = new Exception[]
            {
                new InvalidOperationException("Inner exception 1"),
                new ArgumentException("Inner exception 2")
            };
            var exception = new AggregateException("Aggregate exception", innerExceptions);
            _exitCodeManagerMock.Setup(m => m.GetExitCodeForException(exception))
                .Returns(99);
            _exitCodeManagerMock.Setup(m => m.GetExitCodeDescription(99))
                .Returns("Unexpected error");

            // Act
            int result = _handler.HandleException(exception);

            // Assert
            Assert.Equal(99, result);
            VerifyLoggerCalled(LogLevel.Error, "AggregateException with 2 inner exceptions");
        }

        private void VerifyLoggerCalled(LogLevel level, string messageContains)
        {
            _loggerMock.Verify(
                x => x.Log(
                    It.Is<LogLevel>(l => l == level),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(messageContains)),
                    It.IsAny<Exception?>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.AtLeastOnce);
        }
    }
}

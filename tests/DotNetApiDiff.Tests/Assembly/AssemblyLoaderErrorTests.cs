// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using System;
using System.IO;
using System.Reflection;
using System.Security;
using DotNetApiDiff.AssemblyLoading;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotNetApiDiff.Tests.Assembly
{
    public class AssemblyLoaderErrorTests : IDisposable
    {
        private readonly Mock<ILogger<AssemblyLoader>> _loggerMock;
        private readonly AssemblyLoader _loader;
        private readonly string _tempDir;

        public AssemblyLoaderErrorTests()
        {
            _loggerMock = new Mock<ILogger<AssemblyLoader>>();
            _loader = new AssemblyLoader(_loggerMock.Object);
            _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);
        }

        [Fact]
        public void LoadAssembly_WithNullPath_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => _loader.LoadAssembly(null));
            Assert.Equal("assemblyPath", ex.ParamName);
        }

        [Fact]
        public void LoadAssembly_WithEmptyPath_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => _loader.LoadAssembly(string.Empty));
            Assert.Equal("assemblyPath", ex.ParamName);
        }

        [Fact]
        public void LoadAssembly_WithNonExistentFile_ThrowsFileNotFoundException()
        {
            // Arrange
            string nonExistentPath = Path.Combine(_tempDir, "NonExistent.dll");

            // Act & Assert
            var ex = Assert.Throws<FileNotFoundException>(() => _loader.LoadAssembly(nonExistentPath));
            Assert.Contains(nonExistentPath, ex.Message);
        }

        [Fact]
        public void LoadAssembly_WithInvalidAssembly_ThrowsBadImageFormatException()
        {
            // Arrange
            string invalidAssemblyPath = Path.Combine(_tempDir, "InvalidAssembly.dll");
            File.WriteAllText(invalidAssemblyPath, "This is not a valid assembly");

            // Act & Assert
            Assert.Throws<BadImageFormatException>(() => _loader.LoadAssembly(invalidAssemblyPath));
        }

        [Fact]
        public void LoadAssembly_WithInaccessibleFile_ThrowsIOException()
        {
            // This test only works on Windows where file locking is more strict
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                // Arrange
                string lockedFilePath = Path.Combine(_tempDir, "LockedFile.dll");
                File.WriteAllText(lockedFilePath, "This file will be locked");

                // Create a file lock
                FileStream? lockingStream = null;
                try
                {
                    lockingStream = File.Open(lockedFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

                    // Act & Assert
                    Assert.Throws<IOException>(() => _loader.LoadAssembly(lockedFilePath));
                }
                finally
                {
                    lockingStream?.Dispose();
                }
            }
        }

        [Fact]
        public void IsValidAssembly_WithNullPath_ReturnsFalse()
        {
            // Act
            bool result = _loader.IsValidAssembly(null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidAssembly_WithEmptyPath_ReturnsFalse()
        {
            // Act
            bool result = _loader.IsValidAssembly(string.Empty);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidAssembly_WithNonExistentFile_ReturnsFalse()
        {
            // Arrange
            string nonExistentPath = Path.Combine(_tempDir, "NonExistent.dll");

            // Act
            bool result = _loader.IsValidAssembly(nonExistentPath);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidAssembly_WithInvalidAssembly_ReturnsFalse()
        {
            // Arrange
            string invalidAssemblyPath = Path.Combine(_tempDir, "InvalidAssembly.dll");
            File.WriteAllText(invalidAssemblyPath, "This is not a valid assembly");

            // Act
            bool result = _loader.IsValidAssembly(invalidAssemblyPath);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidAssembly_WithValidAssembly_ReturnsTrue()
        {
            // Arrange - use the current test assembly as a valid assembly
            string validAssemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;

            // Act
            bool result = _loader.IsValidAssembly(validAssemblyPath);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void UnloadAll_DisposesAllLoadContexts()
        {
            // Arrange - load a valid assembly
            string validAssemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            _loader.LoadAssembly(validAssemblyPath);

            // Act - should not throw
            _loader.UnloadAll();

            // Assert - verify logging
            VerifyLoggerCalled(LogLevel.Information, "Unloading all assemblies");
        }

        [Fact]
        public void Dispose_CallsUnloadAll()
        {
            // Act - should not throw
            _loader.Dispose();

            // Assert - verify logging
            VerifyLoggerCalled(LogLevel.Debug, "Disposing AssemblyLoader");
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

        // Implement IDisposable to clean up resources
        public void Dispose()
        {
            // Clean up temp directory
            try
            {
                if (Directory.Exists(_tempDir))
                {
                    Directory.Delete(_tempDir, true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}

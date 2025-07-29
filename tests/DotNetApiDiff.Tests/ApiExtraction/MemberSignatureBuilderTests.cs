// Copyright DotNet API Diff Project Contributors - SPDX Identifier: MIT
using System.Reflection;
using DotNetApiDiff.ApiExtraction;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotNetApiDiff.Tests.ApiExtraction;

public class MemberSignatureBuilderTests
{
    private readonly MemberSignatureBuilder _signatureBuilder;
    private readonly Mock<ILogger<MemberSignatureBuilder>> _mockLogger;

    public MemberSignatureBuilderTests()
    {
        _mockLogger = new Mock<ILogger<MemberSignatureBuilder>>();
        _signatureBuilder = new MemberSignatureBuilder(_mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MemberSignatureBuilder(null!));
    }

    [Fact]
    public void BuildMethodSignature_WithNullMethod_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _signatureBuilder.BuildMethodSignature(null!));
    }

    [Fact]
    public void BuildMethodSignature_WithPublicMethod_ReturnsCorrectSignature()
    {
        // Arrange
        var method = typeof(TestClass).GetMethod(nameof(TestClass.PublicMethod))!;

        // Act
        var signature = _signatureBuilder.BuildMethodSignature(method);

        // Assert
        Assert.Contains("public", signature);
        Assert.Contains("PublicMethod", signature);
        Assert.Contains("void", signature); // Uses C# alias, not System.Void
    }

    [Fact]
    public void BuildMethodSignature_WithPrivateMethod_ReturnsCorrectSignature()
    {
        // Arrange
        var method = typeof(TestClass).GetMethod("PrivateMethod", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Act
        var signature = _signatureBuilder.BuildMethodSignature(method);

        // Assert
        Assert.Contains("private", signature);
        Assert.Contains("PrivateMethod", signature);
    }

    [Fact]
    public void BuildMethodSignature_WithStaticMethod_ReturnsCorrectSignature()
    {
        // Arrange
        var method = typeof(TestClass).GetMethod(nameof(TestClass.StaticMethod))!;

        // Act
        var signature = _signatureBuilder.BuildMethodSignature(method);

        // Assert
        Assert.Contains("static", signature);
        Assert.Contains("StaticMethod", signature);
    }

    [Fact]
    public void BuildMethodSignature_WithVirtualMethod_ReturnsCorrectSignature()
    {
        // Arrange
        var method = typeof(TestClass).GetMethod(nameof(TestClass.VirtualMethod))!;

        // Act
        var signature = _signatureBuilder.BuildMethodSignature(method);

        // Assert
        Assert.Contains("virtual", signature);
        Assert.Contains("VirtualMethod", signature);
    }

    [Fact]
    public void BuildMethodSignature_WithMethodWithParameters_ReturnsCorrectSignature()
    {
        // Arrange
        var method = typeof(TestClass).GetMethod(nameof(TestClass.MethodWithParameters))!;

        // Act
        var signature = _signatureBuilder.BuildMethodSignature(method);

        // Assert
        Assert.Contains("MethodWithParameters", signature);
        Assert.Contains("int", signature); // Uses C# alias, not System.Int32
        Assert.Contains("string", signature); // Uses C# alias, not System.String
    }

    [Fact]
    public void BuildMethodSignature_WithGenericMethod_ReturnsCorrectSignature()
    {
        // Arrange
        var method = typeof(TestClass).GetMethod(nameof(TestClass.GenericMethod))!;

        // Act
        var signature = _signatureBuilder.BuildMethodSignature(method);

        // Assert
        Assert.Contains("GenericMethod", signature);
        Assert.Contains("T", signature);
    }

    [Fact]
    public void BuildPropertySignature_WithNullProperty_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _signatureBuilder.BuildPropertySignature(null!));
    }

    [Fact]
    public void BuildPropertySignature_WithPublicProperty_ReturnsCorrectSignature()
    {
        // Arrange
        var property = typeof(TestClass).GetProperty(nameof(TestClass.PublicProperty))!;

        // Act
        var signature = _signatureBuilder.BuildPropertySignature(property);

        // Assert
        Assert.Contains("public", signature);
        Assert.Contains("PublicProperty", signature);
        Assert.Contains("string", signature); // Uses C# alias, not System.String
        Assert.Contains("get", signature);
        Assert.Contains("set", signature);
    }

    [Fact]
    public void BuildPropertySignature_WithReadOnlyProperty_ReturnsCorrectSignature()
    {
        // Arrange
        var property = typeof(TestClass).GetProperty(nameof(TestClass.ReadOnlyProperty))!;

        // Act
        var signature = _signatureBuilder.BuildPropertySignature(property);

        // Assert
        Assert.Contains("ReadOnlyProperty", signature);
        Assert.Contains("get", signature);
        Assert.DoesNotContain("set", signature);
    }

    [Fact]
    public void BuildFieldSignature_WithNullField_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _signatureBuilder.BuildFieldSignature(null!));
    }

    [Fact]
    public void BuildFieldSignature_WithPublicField_ReturnsCorrectSignature()
    {
        // Arrange
        var field = typeof(TestClass).GetField(nameof(TestClass.PublicField))!;

        // Act
        var signature = _signatureBuilder.BuildFieldSignature(field);

        // Assert
        Assert.Contains("public", signature);
        Assert.Contains("PublicField", signature);
        Assert.Contains("string", signature); // Uses C# alias, not System.String
    }

    [Fact]
    public void BuildFieldSignature_WithStaticField_ReturnsCorrectSignature()
    {
        // Arrange
        var field = typeof(TestClass).GetField(nameof(TestClass.StaticField))!;

        // Act
        var signature = _signatureBuilder.BuildFieldSignature(field);

        // Assert
        Assert.Contains("static", signature);
        Assert.Contains("StaticField", signature);
    }

    [Fact]
    public void BuildFieldSignature_WithReadOnlyField_ReturnsCorrectSignature()
    {
        // Arrange
        var field = typeof(TestClass).GetField(nameof(TestClass.ReadOnlyField))!;

        // Act
        var signature = _signatureBuilder.BuildFieldSignature(field);

        // Assert
        Assert.Contains("readonly", signature);
        Assert.Contains("ReadOnlyField", signature);
    }

    [Fact]
    public void BuildEventSignature_WithNullEvent_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _signatureBuilder.BuildEventSignature(null!));
    }

    [Fact]
    public void BuildEventSignature_WithPublicEvent_ReturnsCorrectSignature()
    {
        // Arrange
        var eventInfo = typeof(TestClass).GetEvent(nameof(TestClass.PublicEvent))!;

        // Act
        var signature = _signatureBuilder.BuildEventSignature(eventInfo);

        // Assert
        Assert.Contains("public", signature);
        Assert.Contains("PublicEvent", signature);
        Assert.Contains("Action", signature); // Uses Action, not System.Action (no namespace prefix for common types)
    }

    [Fact]
    public void BuildConstructorSignature_WithNullConstructor_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _signatureBuilder.BuildConstructorSignature(null!));
    }

    [Fact]
    public void BuildConstructorSignature_WithPublicConstructor_ReturnsCorrectSignature()
    {
        // Arrange
        var constructor = typeof(TestClass).GetConstructor(Type.EmptyTypes)!;

        // Act
        var signature = _signatureBuilder.BuildConstructorSignature(constructor);

        // Assert
        Assert.Contains("public", signature);
        Assert.Contains("TestClass", signature);
    }

    [Fact]
    public void BuildConstructorSignature_WithParameterizedConstructor_ReturnsCorrectSignature()
    {
        // Arrange
        var constructor = typeof(TestClass).GetConstructor(new[] { typeof(string) })!;

        // Act
        var signature = _signatureBuilder.BuildConstructorSignature(constructor);

        // Assert
        Assert.Contains("TestClass", signature);
        Assert.Contains("string", signature); // Uses C# alias, not System.String
    }

    // Test class to provide reflection targets
    private class TestClass
    {
        public string PublicField = string.Empty;
        public static string StaticField = string.Empty;
        public readonly string ReadOnlyField = string.Empty;

        public string PublicProperty { get; set; } = string.Empty;
        public string ReadOnlyProperty { get; } = string.Empty;

        public event Action? PublicEvent;

        public TestClass() { }
        public TestClass(string value) { }

        public void PublicMethod() { }
        private void PrivateMethod() { }
        public static void StaticMethod() { }
        public virtual void VirtualMethod() { }

        public void MethodWithParameters(int number, string text) { }
        public T GenericMethod<T>(T value) => value;
    }
}

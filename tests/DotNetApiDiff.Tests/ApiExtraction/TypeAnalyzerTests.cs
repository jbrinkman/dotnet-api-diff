using System.Reflection;
using DotNetApiDiff.ApiExtraction;
using DotNetApiDiff.Interfaces;
using DotNetApiDiff.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotNetApiDiff.Tests.ApiExtraction;

public class TypeAnalyzerTests
{
    private readonly Mock<IMemberSignatureBuilder> _mockSignatureBuilder;
    private readonly Mock<ILogger<TypeAnalyzer>> _mockLogger;
    private readonly TypeAnalyzer _typeAnalyzer;

    public TypeAnalyzerTests()
    {
        _mockSignatureBuilder = new Mock<IMemberSignatureBuilder>();
        _mockLogger = new Mock<ILogger<TypeAnalyzer>>();
        _typeAnalyzer = new TypeAnalyzer(_mockSignatureBuilder.Object, _mockLogger.Object);
    }

    [Fact]
    public void AnalyzeType_WithClass_ReturnsCorrectApiMember()
    {
        // Arrange
        var type = typeof(TestClass);
        _mockSignatureBuilder.Setup(x => x.BuildTypeSignature(type))
            .Returns("public class TestClass");

        // Act
        var result = _typeAnalyzer.AnalyzeType(type);

        // Assert
        Assert.Equal("TestClass", result.Name);
        Assert.Equal(type.FullName, result.FullName);
        Assert.Equal(type.Namespace, result.Namespace);
        Assert.Equal("public class TestClass", result.Signature);
        Assert.Equal(MemberType.Class, result.Type);
        Assert.Equal(AccessibilityLevel.Public, result.Accessibility);
    }

    [Fact]
    public void AnalyzeType_WithInterface_ReturnsCorrectApiMember()
    {
        // Arrange
        var type = typeof(ITestInterface);
        _mockSignatureBuilder.Setup(x => x.BuildTypeSignature(type))
            .Returns("public interface ITestInterface");

        // Act
        var result = _typeAnalyzer.AnalyzeType(type);

        // Assert
        Assert.Equal("ITestInterface", result.Name);
        Assert.Equal(type.FullName, result.FullName);
        Assert.Equal(type.Namespace, result.Namespace);
        Assert.Equal("public interface ITestInterface", result.Signature);
        Assert.Equal(MemberType.Interface, result.Type);
        Assert.Equal(AccessibilityLevel.Public, result.Accessibility);
    }

    [Fact]
    public void AnalyzeMethods_WithPublicMethods_ReturnsCorrectApiMembers()
    {
        // Arrange
        var type = typeof(TestClass);
        var publicMethod = type.GetMethod("PublicMethod");

        _mockSignatureBuilder.Setup(x => x.BuildMethodSignature(It.IsAny<MethodInfo>()))
            .Returns<MethodInfo>(m => $"public {m.ReturnType.Name} {m.Name}()");

        // Act
        var result = _typeAnalyzer.AnalyzeMethods(type).ToList();

        // Assert
        Assert.Contains(result, m => m.Name == "PublicMethod");
        Assert.DoesNotContain(result, m => m.Name == "PrivateMethod");

        var publicMethodMember = result.First(m => m.Name == "PublicMethod");
        Assert.Equal($"{type.FullName}.PublicMethod", publicMethodMember.FullName);
        Assert.Equal(type.Namespace, publicMethodMember.Namespace);
        Assert.Equal(type.FullName, publicMethodMember.DeclaringType);
        Assert.Equal("public Void PublicMethod()", publicMethodMember.Signature);
        Assert.Equal(MemberType.Method, publicMethodMember.Type);
        Assert.Equal(AccessibilityLevel.Public, publicMethodMember.Accessibility);
    }

    [Fact]
    public void AnalyzeProperties_WithPublicProperties_ReturnsCorrectApiMembers()
    {
        // Arrange
        var type = typeof(TestClass);
        var publicProperty = type.GetProperty("PublicProperty");

        _mockSignatureBuilder.Setup(x => x.BuildPropertySignature(It.IsAny<PropertyInfo>()))
            .Returns<PropertyInfo>(p => $"public {p.PropertyType.Name} {p.Name} {{ get; set; }}");

        // Act
        var result = _typeAnalyzer.AnalyzeProperties(type).ToList();

        // Assert
        Assert.Contains(result, m => m.Name == "PublicProperty");
        Assert.DoesNotContain(result, m => m.Name == "PrivateProperty");

        var publicPropertyMember = result.First(m => m.Name == "PublicProperty");
        Assert.Equal($"{type.FullName}.PublicProperty", publicPropertyMember.FullName);
        Assert.Equal(type.Namespace, publicPropertyMember.Namespace);
        Assert.Equal(type.FullName, publicPropertyMember.DeclaringType);
        Assert.Equal("public String PublicProperty { get; set; }", publicPropertyMember.Signature);
        Assert.Equal(MemberType.Property, publicPropertyMember.Type);
        Assert.Equal(AccessibilityLevel.Public, publicPropertyMember.Accessibility);
    }

    [Fact]
    public void AnalyzeFields_WithPublicFields_ReturnsCorrectApiMembers()
    {
        // Arrange
        var type = typeof(TestClass);
        var publicField = type.GetField("PublicField");

        _mockSignatureBuilder.Setup(x => x.BuildFieldSignature(It.IsAny<FieldInfo>()))
            .Returns<FieldInfo>(f => $"public {f.FieldType.Name} {f.Name}");

        // Act
        var result = _typeAnalyzer.AnalyzeFields(type).ToList();

        // Assert
        Assert.Contains(result, m => m.Name == "PublicField");
        Assert.DoesNotContain(result, m => m.Name == "privateField");

        var publicFieldMember = result.First(m => m.Name == "PublicField");
        Assert.Equal($"{type.FullName}.PublicField", publicFieldMember.FullName);
        Assert.Equal(type.Namespace, publicFieldMember.Namespace);
        Assert.Equal(type.FullName, publicFieldMember.DeclaringType);
        Assert.Equal("public Int32 PublicField", publicFieldMember.Signature);
        Assert.Equal(MemberType.Field, publicFieldMember.Type);
        Assert.Equal(AccessibilityLevel.Public, publicFieldMember.Accessibility);
    }

    [Fact]
    public void AnalyzeEvents_WithPublicEvents_ReturnsCorrectApiMembers()
    {
        // Arrange
        var type = typeof(TestClass);
        var publicEvent = type.GetEvent("PublicEvent");

        _mockSignatureBuilder.Setup(x => x.BuildEventSignature(It.IsAny<EventInfo>()))
            .Returns<EventInfo>(e => $"public event EventHandler {e.Name}");

        // Act
        var result = _typeAnalyzer.AnalyzeEvents(type).ToList();

        // Assert
        Assert.Contains(result, m => m.Name == "PublicEvent");

        var publicEventMember = result.First(m => m.Name == "PublicEvent");
        Assert.Equal($"{type.FullName}.PublicEvent", publicEventMember.FullName);
        Assert.Equal(type.Namespace, publicEventMember.Namespace);
        Assert.Equal(type.FullName, publicEventMember.DeclaringType);
        Assert.Equal("public event EventHandler PublicEvent", publicEventMember.Signature);
        Assert.Equal(MemberType.Event, publicEventMember.Type);
        Assert.Equal(AccessibilityLevel.Public, publicEventMember.Accessibility);
    }

    [Fact]
    public void AnalyzeConstructors_WithPublicConstructors_ReturnsCorrectApiMembers()
    {
        // Arrange
        var type = typeof(TestClass);
        var publicConstructor = type.GetConstructor(Type.EmptyTypes);

        _mockSignatureBuilder.Setup(x => x.BuildConstructorSignature(It.IsAny<ConstructorInfo>()))
            .Returns<ConstructorInfo>(c => $"public {c.DeclaringType.Name}()");

        // Act
        var result = _typeAnalyzer.AnalyzeConstructors(type).ToList();

        // Assert
        Assert.Contains(result, m => m.Name == ".ctor");

        var publicConstructorMember = result.First(m => m.Name == ".ctor");
        Assert.Equal($"{type.FullName}..ctor", publicConstructorMember.FullName);
        Assert.Equal(type.Namespace, publicConstructorMember.Namespace);
        Assert.Equal(type.FullName, publicConstructorMember.DeclaringType);
        Assert.Equal("public TestClass()", publicConstructorMember.Signature);
        Assert.Equal(MemberType.Constructor, publicConstructorMember.Type);
        Assert.Equal(AccessibilityLevel.Public, publicConstructorMember.Accessibility);
    }

    [Fact]
    public void AnalyzeType_WithNullType_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _typeAnalyzer.AnalyzeType(null!));
    }

    // Test classes for reflection tests
    public interface ITestInterface
    {
        void InterfaceMethod();
    }

    public class TestClass : ITestInterface
    {
        public int PublicField;
        private string privateField = "private";

        public string PublicProperty { get; set; }
        private int PrivateProperty { get; set; }

        public event EventHandler PublicEvent;

        public TestClass()
        {
        }

        private TestClass(int value)
        {
        }

        public void PublicMethod()
        {
        }

        private void PrivateMethod()
        {
        }

        public void InterfaceMethod()
        {
        }
    }
}
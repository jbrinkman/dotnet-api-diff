using System.Reflection;
using DotNetApiDiff.ApiExtraction;
using DotNetApiDiff.AssemblyLoading;
using DotNetApiDiff.Interfaces;
using DotNetApiDiff.Models;
using DotNetApiDiff.Models.Configuration;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace DotNetApiDiff.Tests.ApiExtraction
{
    /// <summary>
    /// Integration tests using manually created test assemblies
    /// </summary>
    public class ApiComparerManualTests
    {
        private readonly string _testAssemblyV1Path;
        private readonly string _testAssemblyV2Path;
        private readonly IAssemblyLoader _assemblyLoader;
        private readonly IApiExtractor _apiExtractor;
        private readonly INameMapper _nameMapper;
        private readonly IDifferenceCalculator _differenceCalculator;
        private readonly IChangeClassifier _changeClassifier;
        private readonly IApiComparer _apiComparer;
        private readonly ITestOutputHelper _output;

        public ApiComparerManualTests(ITestOutputHelper output)
        {
            _output = output;

            // For the purpose of this test, we'll use the test assemblies from the TestData directory
            string testDataDir = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty,
                "TestData");

            _testAssemblyV1Path = Path.Combine(testDataDir, "TestAssemblyV1.dll");
            _testAssemblyV2Path = Path.Combine(testDataDir, "TestAssemblyV2.dll");

            _output.WriteLine($"Test assembly V1 path: {_testAssemblyV1Path}");
            _output.WriteLine($"Test assembly V2 path: {_testAssemblyV2Path}");

            // Skip tests if the test assemblies don't exist
            if (!File.Exists(_testAssemblyV1Path) || !File.Exists(_testAssemblyV2Path))
            {
                _output.WriteLine("Test assemblies not found. Skipping tests.");
                return;
            }

            // Create the real components for integration testing
            _assemblyLoader = new AssemblyLoader();
            _apiExtractor = new ApiExtractor(new TypeAnalyzer(new MemberSignatureBuilder()));
            _nameMapper = new NameMapper();
            _differenceCalculator = new DifferenceCalculator();
            _changeClassifier = new ChangeClassifier();
            _apiComparer = new ApiComparer(
                _assemblyLoader,
                _apiExtractor,
                _nameMapper,
                _differenceCalculator,
                _changeClassifier);

            // Create the real components for integration testing
            _assemblyLoader = new AssemblyLoader();
            _apiExtractor = new ApiExtractor(new TypeAnalyzer(new MemberSignatureBuilder()));
            _nameMapper = new NameMapper();
            _differenceCalculator = new DifferenceCalculator();
            _changeClassifier = new ChangeClassifier();
            _apiComparer = new ApiComparer(
                _assemblyLoader,
                _apiExtractor,
                _nameMapper,
                _differenceCalculator,
                _changeClassifier);
        }

        [Fact]
        public void Compare_WithTestAssemblies_DetectsBasicChanges()
        {
            // Skip test if assemblies don't exist
            if (!File.Exists(_testAssemblyV1Path) || !File.Exists(_testAssemblyV2Path))
            {
                _output.WriteLine("Test assemblies not found. Skipping test.");
                return;
            }

            // Arrange
            var config = new ComparisonConfiguration();

            // Act
            var result = _apiComparer.Compare(_testAssemblyV1Path, _testAssemblyV2Path, config);

            // Assert
            Assert.NotNull(result);

            // Verify additions
            Assert.Contains(result.Additions, change =>
                change.TargetMember.Name == "NewMethod" &&
                change.TargetMember.DeclaringType == "TestAssembly.PublicClass");

            Assert.Contains(result.Additions, change =>
                change.TargetMember.Name == "NewProperty" &&
                change.TargetMember.DeclaringType == "TestAssembly.PublicClass");

            Assert.Contains(result.Additions, change =>
                change.TargetMember.Name == "NewField" &&
                change.TargetMember.DeclaringType == "TestAssembly.PublicClass");

            Assert.Contains(result.Additions, change =>
                change.TargetMember.Name == "NewEvent" &&
                change.TargetMember.DeclaringType == "TestAssembly.PublicClass");

            // Verify removals
            Assert.Contains(result.Removals, change =>
                change.SourceMember.Name == "RemovedMethod" &&
                change.SourceMember.DeclaringType == "TestAssembly.PublicClass");

            Assert.Contains(result.Removals, change =>
                change.SourceMember.Name == "RemovedProperty" &&
                change.SourceMember.DeclaringType == "TestAssembly.PublicClass");

            Assert.Contains(result.Removals, change =>
                change.SourceMember.Name == "RemovedField" &&
                change.SourceMember.DeclaringType == "TestAssembly.PublicClass");

            Assert.Contains(result.Removals, change =>
                change.SourceMember.Name == "RemovedEvent" &&
                change.SourceMember.DeclaringType == "TestAssembly.PublicClass");

            // Verify modifications
            Assert.Contains(result.Modifications, change =>
                change.SourceMember.Name == "ChangedSignatureMethod" &&
                change.TargetMember.Name == "ChangedSignatureMethod" &&
                change.SourceMember.DeclaringType == "TestAssembly.PublicClass");

            Assert.Contains(result.Modifications, change =>
                change.SourceMember.Name == "ChangedTypeProperty" &&
                change.TargetMember.Name == "ChangedTypeProperty" &&
                change.SourceMember.DeclaringType == "TestAssembly.PublicClass");

            Assert.Contains(result.Modifications, change =>
                change.SourceMember.Name == "VisibilityChangedMethod" &&
                change.TargetMember.Name == "VisibilityChangedMethod" &&
                change.SourceMember.DeclaringType == "TestAssembly.PublicClass");
        }

        [Fact]
        public void Compare_WithNamespaceMapping_DetectsRenamedNamespace()
        {
            // Skip test if assemblies don't exist
            if (!File.Exists(_testAssemblyV1Path) || !File.Exists(_testAssemblyV2Path))
            {
                _output.WriteLine("Test assemblies not found. Skipping test.");
                return;
            }

            // Arrange
            var config = new ComparisonConfiguration
            {
                NamespaceMappings = new Dictionary<string, List<string>>
                {
                    { "TestAssembly.NamespaceToBeRenamed", new List<string> { "TestAssembly.RenamedNamespace" } }
                }
            };

            // Act
            var result = _apiComparer.Compare(_testAssemblyV1Path, _testAssemblyV2Path, config);

            // Assert
            Assert.NotNull(result);

            // Verify that the class in the renamed namespace is not reported as removed
            Assert.DoesNotContain(result.Removals, change =>
                change.SourceMember.DeclaringType == "TestAssembly.NamespaceToBeRenamed.ClassInRenamedNamespace");

            // Verify that the class in the renamed namespace is not reported as added
            Assert.DoesNotContain(result.Additions, change =>
                change.TargetMember.DeclaringType == "TestAssembly.RenamedNamespace.ClassInRenamedNamespace");

            // Verify that the members of the class are correctly mapped
            Assert.DoesNotContain(result.Removals, change =>
                change.SourceMember.Name == "Method" &&
                change.SourceMember.DeclaringType == "TestAssembly.NamespaceToBeRenamed.ClassInRenamedNamespace");

            Assert.DoesNotContain(result.Removals, change =>
                change.SourceMember.Name == "Property" &&
                change.SourceMember.DeclaringType == "TestAssembly.NamespaceToBeRenamed.ClassInRenamedNamespace");
        }

        [Fact]
        public void Compare_WithExclusions_ExcludesSpecifiedMembers()
        {
            // Skip test if assemblies don't exist
            if (!File.Exists(_testAssemblyV1Path) || !File.Exists(_testAssemblyV2Path))
            {
                _output.WriteLine("Test assemblies not found. Skipping test.");
                return;
            }

            // Arrange
            var config = new ComparisonConfiguration
            {
                ExcludedMembers = new List<string>
                {
                    "TestAssembly.PublicClass.RemovedMethod",
                    "TestAssembly.PublicClass.RemovedProperty",
                    "TestAssembly.IPublicInterface.RemovedMethod"
                }
            };

            // Act
            var result = _apiComparer.Compare(_testAssemblyV1Path, _testAssemblyV2Path, config);

            // Assert
            Assert.NotNull(result);

            // Verify that excluded members are not reported as removed
            Assert.DoesNotContain(result.Removals, change =>
                change.SourceMember.Name == "RemovedMethod" &&
                change.SourceMember.DeclaringType == "TestAssembly.PublicClass");

            Assert.DoesNotContain(result.Removals, change =>
                change.SourceMember.Name == "RemovedProperty" &&
                change.SourceMember.DeclaringType == "TestAssembly.PublicClass");

            Assert.DoesNotContain(result.Removals, change =>
                change.SourceMember.Name == "RemovedMethod" &&
                change.SourceMember.DeclaringType == "TestAssembly.IPublicInterface");

            // Verify that excluded members are reported in the Excluded collection
            Assert.Contains(result.Excluded, change =>
                change.SourceMember.Name == "RemovedMethod" &&
                change.SourceMember.DeclaringType == "TestAssembly.PublicClass");

            Assert.Contains(result.Excluded, change =>
                change.SourceMember.Name == "RemovedProperty" &&
                change.SourceMember.DeclaringType == "TestAssembly.PublicClass");

            Assert.Contains(result.Excluded, change =>
                change.SourceMember.Name == "RemovedMethod" &&
                change.SourceMember.DeclaringType == "TestAssembly.IPublicInterface");
        }

        [Fact]
        public void Compare_WithBreakingChangeRules_ClassifiesBreakingChanges()
        {
            // Skip test if assemblies don't exist
            if (!File.Exists(_testAssemblyV1Path) || !File.Exists(_testAssemblyV2Path))
            {
                _output.WriteLine("Test assemblies not found. Skipping test.");
                return;
            }

            // Arrange
            var config = new ComparisonConfiguration
            {
                BreakingChangeRules = new BreakingChangeRules
                {
                    TreatMemberRemovalAsBreaking = true,
                    TreatSignatureChangeAsBreaking = true,
                    TreatVisibilityDecreaseAsBreaking = true,
                    TreatPropertyTypeChangeAsBreaking = true
                }
            };

            // Act
            var result = _apiComparer.Compare(_testAssemblyV1Path, _testAssemblyV2Path, config);

            // Assert
            Assert.NotNull(result);

            // Verify that removals are classified as breaking changes
            foreach (var removal in result.Removals)
            {
                Assert.True(removal.IsBreakingChange);
            }

            // Verify that signature changes are classified as breaking changes
            Assert.Contains(result.Modifications, change =>
                change.SourceMember.Name == "ChangedSignatureMethod" &&
                change.IsBreakingChange);

            // Verify that property type changes are classified as breaking changes
            Assert.Contains(result.Modifications, change =>
                change.SourceMember.Name == "ChangedTypeProperty" &&
                change.IsBreakingChange);

            // Verify that visibility increases are not classified as breaking changes
            Assert.Contains(result.Modifications, change =>
                change.SourceMember.Name == "VisibilityChangedMethod" &&
                !change.IsBreakingChange);
        }
    }
}

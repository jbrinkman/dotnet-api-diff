# Test Assemblies for Integration Testing

This directory contains test assemblies used for integration testing of the DotNetApiDiff tool.

## Test Assembly Structure

### TestAssemblyV1.dll

Contains the following API elements:

- `TestAssembly.PublicClass`
  - `UnchangedMethod()` - Remains unchanged in V2
  - `RemovedMethod()` - Removed in V2
  - `ChangedSignatureMethod(string)` - Signature changed in V2
  - `UnchangedProperty` - Remains unchanged in V2
  - `RemovedProperty` - Removed in V2
  - `ChangedTypeProperty` (string) - Type changed in V2
  - `UnchangedField` - Remains unchanged in V2
  - `RemovedField` - Removed in V2
  - `UnchangedEvent` - Remains unchanged in V2
  - `RemovedEvent` - Removed in V2
  - `VisibilityChangedMethod()` (protected) - Visibility changed to public in V2

- `TestAssembly.IPublicInterface`
  - `UnchangedMethod()` - Remains unchanged in V2
  - `RemovedMethod()` - Removed in V2
  - `UnchangedProperty` - Remains unchanged in V2
  - `RemovedProperty` - Removed in V2
  - `UnchangedEvent` - Remains unchanged in V2
  - `RemovedEvent` - Removed in V2

- `TestAssembly.NamespaceToBeRenamed.ClassInRenamedNamespace`
  - `Method()` - Remains unchanged in V2 (but in renamed namespace)
  - `Property` - Remains unchanged in V2 (but in renamed namespace)

### TestAssemblyV2.dll

Contains the following API elements:

- `TestAssembly.PublicClass`
  - `UnchangedMethod()` - Unchanged from V1
  - `ChangedSignatureMethod(int)` - Signature changed from V1
  - `NewMethod()` - Added in V2
  - `UnchangedProperty` - Unchanged from V1
  - `ChangedTypeProperty` (int) - Type changed from V1
  - `NewProperty` - Added in V2
  - `UnchangedField` - Unchanged from V1
  - `NewField` - Added in V2
  - `UnchangedEvent` - Unchanged from V1
  - `NewEvent` - Added in V2
  - `VisibilityChangedMethod()` (public) - Visibility changed from protected in V1

- `TestAssembly.IPublicInterface`
  - `UnchangedMethod()` - Unchanged from V1
  - `NewMethod()` - Added in V2
  - `UnchangedProperty` - Unchanged from V1
  - `NewProperty` - Added in V2
  - `UnchangedEvent` - Unchanged from V1
  - `NewEvent` - Added in V2

- `TestAssembly.RenamedNamespace.ClassInRenamedNamespace`
  - `Method()` - Unchanged from V1 (but in renamed namespace)
  - `Property` - Unchanged from V1 (but in renamed namespace)

## Building the Test Assemblies

The test assemblies need to be built separately and placed in this directory for the integration tests to work.

```bash
# Build TestAssemblyV1
dotnet build TestAssemblyV1.csproj -c Release

# Build TestAssemblyV2
dotnet build TestAssemblyV2.csproj -c Release

# Copy the assemblies to the TestData directory
cp bin/Release/net8.0/TestAssemblyV1.dll ../DotNetApiDiff.Tests/TestData/
cp bin/Release/net8.0/TestAssemblyV2.dll ../DotNetApiDiff.Tests/TestData/
```

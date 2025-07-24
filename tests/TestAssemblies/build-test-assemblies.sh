#!/bin/bash

# Build the test assemblies
echo "Building TestAssemblyV1..."
dotnet build TestAssemblyV1.csproj -c Release

echo "Building TestAssemblyV2..."
dotnet build TestAssemblyV2.csproj -c Release

# Copy the assemblies to the TestData directory for integration tests
echo "Copying assemblies to TestData directory..."
cp bin/Release/net8.0/TestAssemblyV1.dll ../DotNetApiDiff.Tests/TestData/
cp bin/Release/net8.0/TestAssemblyV2.dll ../DotNetApiDiff.Tests/TestData/

echo "Done building and copying test assemblies."

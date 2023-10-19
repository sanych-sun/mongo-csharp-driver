#!/usr/bin/env bash
set -o errexit  # Exit the script with error if any of the commands fail

echo "Configure dotnet cli to use local manifest"
dotnet new tool-manifest --force

echo "Installing docfx tool"
dotnet tool install docfx --version "2.71.1" --local --verbosity q

echo "Building the api-docs"
dotnet tool run docfx metadata ./docfx_project/docfx.json --property ProduceReferenceAssembly=true
dotnet tool run docfx build ./docfx_project/docfx.json -o:./build/api-docs

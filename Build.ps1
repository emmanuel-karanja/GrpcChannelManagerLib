<#
.SYNOPSIS
    Builds the GrpcChannelManagerLib project and runs tests.
.DESCRIPTION
    Restores dependencies, optionally cleans, builds the library, runs unit tests, and optionally packs a NuGet package.
#>

param (
    [string]$ProjectPath = ".\GrpcChannelManagerLib\GrpcChannelManagerLib.csproj",
    [string]$TestProjectPath = ".\GrpcChannelManagerLib.Tests\GrpcChannelManagerLib.Tests.csproj",
    [string]$Configuration = "Release",
    [switch]$Pack,
    [switch]$Clean
)

# Ensure .NET SDK is installed
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error "dotnet SDK not found. Please install .NET SDK to continue."
    exit 1
}

# Clean if requested
if ($Clean) {
    Write-Host "Cleaning project $ProjectPath ..."
    dotnet clean $ProjectPath --configuration $Configuration
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    if (Test-Path $TestProjectPath) {
        Write-Host "Cleaning test project $TestProjectPath ..."
        dotnet clean $TestProjectPath --configuration $Configuration
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }
}

Write-Host "Restoring packages for $ProjectPath ..."
dotnet restore $ProjectPath
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Building $ProjectPath in $Configuration configuration ..."
dotnet build $ProjectPath --configuration $Configuration
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Build and run the test project
if (Test-Path $TestProjectPath) {
    Write-Host "Building test project $TestProjectPath ..."
    dotnet build $TestProjectPath --configuration $Configuration
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host "Running tests for $TestProjectPath ..."
    dotnet test $TestProjectPath --configuration $Configuration --logger "console;verbosity=detailed"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
} else {
    Write-Warning "Test project not found at $TestProjectPath. Skipping tests."
}

if ($Pack) {
    Write-Host "Packing $ProjectPath as NuGet package ..."
    dotnet pack $ProjectPath --configuration $Configuration --output ".\nupkg"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Write-Host "NuGet package created in .\nupkg"
}

Write-Host "Build and test completed successfully."

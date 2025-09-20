<#
.SYNOPSIS
    Builds SampleGrpcMicroservice as a self-contained EXE, ensures Redis is running, then runs the EXE.
.DESCRIPTION
    Restores dependencies, publishes the sample microservice, starts Redis via docker-compose,
    waits for Redis to be ready, and runs the EXE.
#>

param (
    [string]$SolutionRoot = ".",
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",  # Change to linux-x64 if on Linux
    [string]$DockerComposeFile = ".\docker-compose.yml"
)

# Paths
$sampleProject = Join-Path $SolutionRoot "SampleGrpcMicroservice\SampleGrpcMicroservice.csproj"
$publishDir = Join-Path $SolutionRoot "SampleGrpcMicroservice\bin\$Configuration\net8.0\$Runtime\publish"
$exePath = Join-Path $publishDir "SampleGrpcMicroservice.exe"

# ----------------------------
# Step 0: Prerequisites
# ----------------------------
Write-Host "`n=== Checking prerequisites ==="

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error "❌ .NET SDK not found. Please install .NET SDK to continue."
    exit 1
} else {
    Write-Host "✅ .NET SDK found."
}

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Error "❌ Docker not found. Please install Docker to continue."
    exit 1
} else {
    Write-Host "✅ Docker CLI found."
}

# ----------------------------
# Step 1: Publish the microservice
# ----------------------------
Write-Host "`n=== Publishing SampleGrpcMicroservice as self-contained EXE ==="
dotnet publish $sampleProject -c $Configuration -r $Runtime --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=true
if ($LASTEXITCODE -ne 0) {
    Write-Error "❌ Failed to publish SampleGrpcMicroservice."
    exit $LASTEXITCODE
} else {
    Write-Host "✅ Successfully published EXE to: $publishDir"
}

# ----------------------------
# Step 2: Start Redis via docker-compose
# ----------------------------
Write-Host "`n=== Starting Redis via docker-compose ==="
if (-Not (Test-Path $DockerComposeFile)) {
    Write-Error "❌ docker-compose.yml not found at $DockerComposeFile"
    exit 1
}

docker-compose -f $DockerComposeFile up -d
if ($LASTEXITCODE -ne 0) {
    Write-Error "❌ Failed to start Redis via docker-compose."
    exit $LASTEXITCODE
} else {
    Write-Host "✅ Redis container started (detached)."
}

# ----------------------------
# Step 3: Wait for Redis to become ready
# ----------------------------
Write-Host "`n=== Waiting for Redis to be ready ==="
$maxRetries = 10
$retry = 0
$redisReady = $false

while ($retry -lt $maxRetries -and -not $redisReady) {
    try {
        $connection = [StackExchange.Redis.ConnectionMultiplexer]::Connect("localhost:6379")
        if ($connection.IsConnected) {
            $redisReady = $true
            $connection.Close()
        }
    } catch {}
    if (-not $redisReady) {
        Write-Host "⏳ Redis not ready yet (attempt $($retry + 1)/$maxRetries). Waiting 3 seconds..."
        Start-Sleep -Seconds 3
        $retry++
    }
}

if (-not $redisReady) {
    Write-Warning "⚠ Redis did not become ready after $maxRetries retries. Proceeding anyway..."
} else {
    Write-Host "✅ Redis is ready!"
}

# ----------------------------
# Step 4: Run the microservice EXE
# ----------------------------
Write-Host "`n=== Running SampleGrpcMicroservice.exe ==="
if (-not (Test-Path $exePath)) {
    Write-Error "❌ Published EXE not found at $exePath"
    exit 1
}

Write-Host "▶ Starting SampleGrpcMicroservice.exe..."
Start-Process -FilePath $exePath
Write-Host "✅ SampleGrpcMicroservice.exe process started."

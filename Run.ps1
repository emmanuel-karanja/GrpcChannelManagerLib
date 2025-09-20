<#
.SYNOPSIS
    Builds SampleGrpcMicroservice as a self-contained EXE, ensures Redis is running, then runs the EXE.
.DESCRIPTION
    Restores dependencies, publishes the sample microservice, starts Redis via docker-compose,
    waits for Redis to be ready using container health status, and runs the EXE.
#>

param (
    [string]$SolutionRoot = ".",
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",  # Change to linux-x64 if on Linux
    [string]$DockerComposeFile = ".\docker-compose.yml",
    [int]$MaxRedisRetries = 10,
    [int]$RedisRetryDelaySeconds = 3
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
# Step 3: Wait for Redis to become healthy
# ----------------------------
Write-Host "`n=== Waiting for Redis container to be healthy ==="
$retry = 0
$redisReady = $false
$redisContainerName = "sample-redis"

while ($retry -lt $MaxRedisRetries -and -not $redisReady) {
    try {
        $status = docker inspect --format='{{.State.Health.Status}}' $redisContainerName 2>$null
        if ($status -eq "healthy") {
            $redisReady = $true
        } else {
            Write-Host "⏳ Redis not ready yet (attempt $($retry+1)/$MaxRedisRetries). Status: $status. Waiting $RedisRetryDelaySeconds seconds..."
            Start-Sleep -Seconds $RedisRetryDelaySeconds
            $retry++
        }
    } catch {
        Write-Host "⏳ Redis container not found yet. Waiting $RedisRetryDelaySeconds seconds..."
        Start-Sleep -Seconds $RedisRetryDelaySeconds
        $retry++
    }
}

if (-not $redisReady) {
    Write-Warning "⚠ Redis did not become healthy after $MaxRedisRetries retries. Proceeding anyway..."
} else {
    Write-Host "✅ Redis is healthy and ready!"
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

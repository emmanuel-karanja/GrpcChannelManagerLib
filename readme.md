# SampleGrpcMicroservice

## Overview

This repository contains a production-ready **gRPC microservice** sample with dynamic endpoint management. It demonstrates:

* gRPC channel management with dynamic host\:port updates.
* Redis integration for publishing and subscribing to gRPC endpoint updates.
* Optional Azure KeyVault integration for storing configuration.
* Self-contained EXE publishing for Windows and Linux.
* Docker Compose setup for Redis for local testing.

## Project Structure

```
GrpcChannelManagerLib/           # Library for managing gRPC channels
SampleGrpcMicroservice/          # Sample microservice project
docker-compose.yml               # Redis container setup (place in root folder)
BuildAndRun-SampleGrpcMicroservice.ps1  # PowerShell script to build and run sample
```

## Prerequisites

* .NET 8 SDK
* Docker (for Redis container)
* PowerShell (for running scripts)

## Running the Sample

### Option 1: Build and run via PowerShell script

```powershell
.\BuildAndRun-SampleGrpcMicroservice.ps1
```

This script will:

1. Publish the SampleGrpcMicroservice as a self-contained EXE.
2. Start Redis via docker-compose located in the root folder.
3. Wait for Redis to be ready.
4. Run the EXE with dynamic gRPC endpoint management.

### Option 2: Docker Compose only

Start Redis for local testing:

```bash
docker-compose up -d
```

Then run the published EXE manually:

```powershell
SampleGrpcMicroservice\bin\Release\net8.0\win-x64\publish\SampleGrpcMicroservice.exe
```

## Environment Variables

* `REDIS_CONNECTION` : Redis host and port (default: `localhost:6379`).
* `REDIS_CHANNEL` : Redis pub/sub channel for endpoints (default: `GrpcEndpointsChannel`).

## Extending the Sample

* Add more gRPC endpoints in `GrpcChannelManagerLib`.
* Connect multiple microservices to the same Redis channel.
* Integrate Azure KeyVault or other configuration stores for dynamic endpoints.

## Notes

* Place `docker-compose.yml` in the root of the repository for consistent detection by scripts.
* The sample microservice auto-detects Redis at `localhost:6379` if running locally.
* The EXE can run on Windows or Linux depending on the runtime specified during publishing.
* Docker Compose includes a health check for Redis to ensure readiness.

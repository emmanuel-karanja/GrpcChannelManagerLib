# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the library and microservice projects
COPY GrpcChannelManagerLib ./GrpcChannelManagerLib
COPY SampleGrpcMicroservice ./SampleGrpcMicroservice

# Optionally, create a solution to make restore easier
RUN dotnet new sln -n MySolution
RUN dotnet sln add GrpcChannelManagerLib/GrpcChannelManagerLib.csproj
RUN dotnet sln add SampleGrpcMicroservice/SampleGrpcMicroservice.csproj

# Restore, build, and publish the microservice as self-contained Linux executable
WORKDIR /src/SampleGrpcMicroservice
RUN dotnet restore
RUN dotnet publish -c Release -r linux-x64 --self-contained true -o /app/publish /p:PublishSingleFile=true /p:PublishTrimmed=true

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/runtime-deps:8.0
WORKDIR /app

# Copy the published app from the build stage
COPY --from=build /app/publish .

# Expose ports used by gRPC microservice
EXPOSE 5000 5001

# Set entrypoint to the Linux executable
ENTRYPOINT ["./SampleGrpcMicroservice"]

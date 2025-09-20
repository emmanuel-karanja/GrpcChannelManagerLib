using GrpcChannelManagerLib.Extensions;
using GrpcChannelManagerLib.Interfaces;
using GrpcChannelManagerLib.Options;
using GrpcChannelManagerLib.Providers;
using StackExchange.Redis;
using Serilog;

// Configure Serilog to log to console and file
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("logs/GrpcApp.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5015); // HTTP
    options.ListenAnyIP(5016, listenOptions => listenOptions.UseHttps()); // HTTPS (optional)
});


// Use Serilog as logging provider
builder.Host.UseSerilog();

// Configure gRPC Channel Manager with initial endpoints
builder.Services.AddGrpcChannelManager(options =>
{
    options.Endpoints = new List<string> { "https://localhost:5011", "https://localhost:5012" };
});

// Detect local Redis (default Docker container: sample-redis)
string redisConnectionString = "localhost:6379";
string redisChannelName = "GrpcEndpointsChannel";

try
{
    using var testConnection = ConnectionMultiplexer.Connect(redisConnectionString);
    testConnection.Close();

    builder.Services.AddSingleton<RedisConfigProvider>(sp =>
    {
        var logger = sp.GetRequiredService<ILogger<RedisConfigProvider>>();
        return new RedisConfigProvider(redisConnectionString, redisChannelName, logger);
    });

    Log.Information("Redis detected at {RedisConnection}, using it for dynamic endpoints.", redisConnectionString);
}
catch (Exception ex)
{
    Log.Warning(ex, "Redis not available at {RedisConnection}. Dynamic updates via Redis disabled.", redisConnectionString);
}

var app = builder.Build();

// Access gRPC Channel Manager
var channelManager = app.Services.GetRequiredService<IGrpcChannelManager>();

// Use Redis if registered
var redisProvider = app.Services.GetService<RedisConfigProvider>();
if (redisProvider != null)
{
    try
    {
        redisProvider.Subscribe(endpoints =>
        {
            try
            {
                Log.Information("Updating gRPC endpoints from Redis: {Endpoints}", string.Join(", ", endpoints));
                channelManager.UpdateEndpoints(endpoints);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to update gRPC endpoints from Redis");
            }
        });
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to subscribe to Redis channel");
    }
}

// Demo HTTP endpoints for testing Redis updates
app.MapGet("/grpc-test", () =>
{
    try
    {
        var channel = channelManager.GetAllChannels().FirstOrDefault();
        return channel != null
            ? Results.Ok($"Channel ready: {channel.Target}")
            : Results.NotFound();
    }
    catch (Exception ex)
    {
        Log.Error(ex, "/grpc-test failed");
        return Results.Problem("Failed to retrieve gRPC channel.");
    }
});

app.MapPost("/update-endpoints", (List<string> newEndpoints) =>
{
    try
    {
        channelManager.UpdateEndpoints(newEndpoints);
        redisProvider?.Publish(newEndpoints); // broadcast updates via Redis if available
        return Results.Ok("Endpoints updated and broadcasted via Redis.");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "/update-endpoints failed");
        return Results.Problem("Failed to update endpoints.");
    }
});

app.Run();

using GrpcChannelManagerLib.Extensions;
using GrpcChannelManagerLib.Interfaces;
using GrpcChannelManagerLib.Options;
using GrpcChannelManagerLib.Providers;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Configure gRPC Channel Manager with initial endpoints
builder.Services.AddGrpcChannelManager(options =>
{
    options.Endpoints = new List<string> { "https://localhost:5001", "https://localhost:5002" };
});

// Register KeyVault provider using Azure DefaultAzureCredential
builder.Services.AddSingleton<KeyVaultConfigProvider>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<KeyVaultConfigProvider>>();
    var vaultUri = "https://<your-vault-name>.vault.azure.net/";
    return new KeyVaultConfigProvider(vaultUri, logger);
});

// Detect local Redis (default Docker container: sample-redis)
string redisConnectionString = "localhost:6379";
string redisChannelName = "GrpcEndpointsChannel";

try
{
    var testConnection = StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnectionString);
    testConnection.Close();
    builder.Services.AddSingleton<RedisConfigProvider>(sp =>
        new RedisConfigProvider(redisConnectionString, redisChannelName, sp.GetRequiredService<ILogger<RedisConfigProvider>>())
    );
    Console.WriteLine($"[Info] Redis detected at {redisConnectionString}, using it for dynamic endpoints.");
}
catch
{
    Console.WriteLine("[Warning] Redis not available at localhost:6379. Dynamic updates via Redis disabled.");
}

var app = builder.Build();

// Access gRPC Channel Manager
var channelManager = app.Services.GetRequiredService<IGrpcChannelManager>();

// Try to use Redis if registered
var redisProvider = app.Services.GetService<RedisConfigProvider>();
if (redisProvider != null)
{
    redisProvider.Subscribe(endpoints =>
    {
        Console.WriteLine($"[Redis] Updating gRPC endpoints: {string.Join(", ", endpoints)}");
        channelManager.UpdateEndpoints(endpoints);
    });
}

// Periodically fetch endpoints from Azure KeyVault
var keyVaultProvider = app.Services.GetRequiredService<KeyVaultConfigProvider>();
_ = Task.Run(async () =>
{
    while (true)
    {
        var endpoints = await keyVaultProvider.GetEndpointsAsync("GrpcEndpointsSecret");
        if (endpoints.Any())
        {
            Console.WriteLine($"[KeyVault] Updating gRPC endpoints: {string.Join(", ", endpoints)}");
            channelManager.UpdateEndpoints(endpoints);
        }
        await Task.Delay(TimeSpan.FromSeconds(60));
    }
});

// Demo HTTP endpoints
app.MapGet("/grpc-test", () =>
{
    var channel = channelManager.GetAllChannels().FirstOrDefault();
    return channel != null
        ? Results.Ok($"Channel ready: {channel.Target}")
        : Results.NotFound();
});

app.MapPost("/update-endpoints", (List<string> newEndpoints) =>
{
    channelManager.UpdateEndpoints(newEndpoints);
    redisProvider?.Publish(newEndpoints); // broadcast updates via Redis if available
    return Results.Ok("Endpoints updated and broadcasted via Redis.");
});

app.Run();

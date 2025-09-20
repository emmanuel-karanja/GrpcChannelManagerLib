using GrpcChannelManagerLib.Extensions;
using GrpcChannelManagerLib.Providers;
using Serilog;
using StackExchange.Redis;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("logs/GrpcApp.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5015); // HTTP
    options.ListenAnyIP(5016, listenOptions => listenOptions.UseHttps());
});

builder.Host.UseSerilog();

// 🔹 Register controllers from this assembly
builder.Services.AddControllers()
       .AddApplicationPart(typeof(Program).Assembly);

// 🔹 gRPC Channel Manager
builder.Services.AddGrpcChannelManager(options =>
{
    options.Endpoints = new List<string> { "https://localhost:5011", "https://localhost:5012" };
});

// 🔹 Optional Redis
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

    Log.Information("✅ Redis detected at {RedisConnection}", redisConnectionString);
}
catch (Exception ex)
{
    Log.Warning(ex, "⚠ Redis not available at {RedisConnection}", redisConnectionString);
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.MapControllers();

// 🔎 Debug endpoint to list all routes
app.MapGet("/routes", (IEnumerable<EndpointDataSource> sources) =>
    string.Join("\n", sources.SelectMany(s => s.Endpoints).Select(e => e.DisplayName))
);

app.Run();

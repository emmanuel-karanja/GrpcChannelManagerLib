using StackExchange.Redis;
using Microsoft.Extensions.Logging;

namespace GrpcChannelManagerLib.Providers;

public class RedisConfigProvider
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisConfigProvider> _logger;
    private readonly string _channelName;

    public RedisConfigProvider(string connectionString, string channelName, ILogger<RedisConfigProvider> logger)
    {
        _logger = logger;
        _channelName = channelName;
        _redis = ConnectionMultiplexer.Connect(connectionString);
    }

    /// <summary>
    /// Subscribe to Redis channel to receive dynamic endpoint updates.
    /// </summary>
    public void Subscribe(Action<List<string>> onEndpointsUpdated)
    {
        var subscriber = _redis.GetSubscriber();
        subscriber.Subscribe(_channelName, (channel, message) =>
        {
            _logger.LogInformation("Received Redis endpoint update: {Message}", message);
            var endpoints = message.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            onEndpointsUpdated(endpoints);
        });
    }

    /// <summary>
    /// Publish a new endpoint list to all subscribers.
    /// </summary>
    public void Publish(List<string> endpoints)
    {
        var subscriber = _redis.GetSubscriber();
        var message = string.Join(',', endpoints);
        subscriber.Publish(_channelName, message);
        _logger.LogInformation("Published gRPC endpoints to Redis channel: {Message}", message);
    }
}

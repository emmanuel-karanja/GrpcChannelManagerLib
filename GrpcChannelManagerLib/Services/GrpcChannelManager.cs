using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using GrpcChannelManagerLib.Interfaces;
using GrpcChannelManagerLib.Options;

namespace GrpcChannelManagerLib.Services;

public class GrpcChannelManager : IGrpcChannelManager
{
    private readonly ConcurrentDictionary<string, GrpcChannel> _channels = new();
    private readonly ILogger<GrpcChannelManager> _logger;
    private GrpcServerOptions _options;

    public GrpcChannelManager(IOptionsMonitor<GrpcServerOptions> optionsMonitor, ILogger<GrpcChannelManager> logger)
    {
        _logger = logger;
        _options = optionsMonitor.CurrentValue;

        foreach (var endpoint in _options.Endpoints)
            GetOrCreateChannel(endpoint);

        optionsMonitor.OnChange(updatedOptions => UpdateEndpoints(updatedOptions.Endpoints));
    }

    public GrpcChannel GetOrCreateChannel(string address)
    {
        return _channels.GetOrAdd(address, addr =>
        {
            _logger.LogInformation("Creating gRPC channel for {Address}", addr);
            return GrpcChannel.ForAddress(addr);
        });
    }

    public void RemoveChannel(string address)
    {
        if (_channels.TryRemove(address, out var channel))
        {
            _logger.LogInformation("Disposing gRPC channel for {Address}", address);
            channel.Dispose();
        }
    }

    public IEnumerable<GrpcChannel> GetAllChannels() => _channels.Values;

    public void UpdateEndpoints(IEnumerable<string> newEndpoints)
    {
        var newSet = new HashSet<string>(newEndpoints);

        foreach (var key in _channels.Keys)
            if (!newSet.Contains(key)) RemoveChannel(key);

        foreach (var endpoint in newSet)
            GetOrCreateChannel(endpoint);
    }
}

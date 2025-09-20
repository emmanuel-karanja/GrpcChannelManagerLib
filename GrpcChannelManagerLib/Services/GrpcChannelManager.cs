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
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = optionsMonitor.CurrentValue ?? throw new ArgumentNullException(nameof(optionsMonitor.CurrentValue));

        // Validate and create channels for all initial endpoints
        foreach (var endpoint in _options.Endpoints)
        {
            GetOrCreateChannel(endpoint);
        }

        // Subscribe to changes
        optionsMonitor.OnChange(updatedOptions =>
        {
            UpdateEndpoints(updatedOptions.Endpoints);
        });
    }

    public GrpcChannel GetOrCreateChannel(string address)
    {
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri))
            throw new ArgumentException($"Invalid gRPC endpoint URI: '{address}'", nameof(address));

        // Use absolute URI (without trailing slash) as the key
        var normalized = uri.GetLeftPart(UriPartial.Path).TrimEnd('/');

        return _channels.GetOrAdd(normalized, _ =>
        {
            _logger.LogInformation("Creating gRPC channel for {Address}", normalized);
            return GrpcChannel.ForAddress(normalized);
        });
    }

    public void RemoveChannel(string address)
    {
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri))
            throw new ArgumentException($"Invalid gRPC endpoint URI: '{address}'", nameof(address));

        var normalized = uri.GetLeftPart(UriPartial.Path).TrimEnd('/');

        if (_channels.TryRemove(normalized, out var channel))
        {
            _logger.LogInformation("Disposing gRPC channel for {Address}", normalized);
            channel.Dispose();
        }
    }


    public IEnumerable<GrpcChannel> GetAllChannels() => _channels.Values;

    public void UpdateEndpoints(IEnumerable<string> newEndpoints)
    {
        if (newEndpoints == null) return;

        var normalizedNew = new HashSet<string>(newEndpoints.Select(NormalizeAddress));

        // Remove channels that are no longer needed
        foreach (var key in _channels.Keys)
        {
            if (!normalizedNew.Contains(key)) RemoveChannel(key);
        }

        // Add new channels
        foreach (var endpoint in normalizedNew)
        {
            GetOrCreateChannel(endpoint);
        }
    }

    /// <summary>
    /// Ensures the endpoint is trimmed and has a valid scheme.
    /// </summary>
    private static string NormalizeAddress(string address)
    {
        address = address.Trim();

        if (!address.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !address.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Invalid gRPC endpoint URI: '{address}'");
        }

        // Validate the URI
        var uri = new Uri(address); // Will throw if invalid
        return uri.AbsoluteUri;     // Always normalized
    }
}

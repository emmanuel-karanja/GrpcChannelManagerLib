using Grpc.Net.Client;
using GrpcChannelManagerLib.Interfaces;

namespace GrpcChannelManagerLib.Services;

public class GrpcClientFactory<TClient> : IGrpcClientFactory<TClient>
    where TClient : Grpc.Core.ClientBase<TClient>
{
    private readonly IGrpcChannelManager _channelManager;
    private readonly Func<GrpcChannel, TClient> _clientCreator;

    public GrpcClientFactory(IGrpcChannelManager channelManager, Func<GrpcChannel, TClient> clientCreator)
    {
        _channelManager = channelManager;
        _clientCreator = clientCreator;
    }

    public TClient CreateClient(string address)
    {
        var channel = _channelManager.GetOrCreateChannel(address);
        return _clientCreator(channel);
    }

    public TClient CreateClientFromConfiguredEndpoints()
    {
        var endpoints = _channelManager.GetAllChannels().Select(c => c.Target);
        var selected = endpoints.First(); // round-robin or random
        return CreateClient(selected);
    }
}

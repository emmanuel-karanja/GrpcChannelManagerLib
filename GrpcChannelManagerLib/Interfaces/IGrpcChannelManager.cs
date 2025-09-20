using Grpc.Net.Client;

namespace GrpcChannelManagerLib.Interfaces;

public interface IGrpcChannelManager
{
    GrpcChannel GetOrCreateChannel(string address);
    void RemoveChannel(string address);
    IEnumerable<GrpcChannel> GetAllChannels();
    void UpdateEndpoints(IEnumerable<string> newEndpoints);
}

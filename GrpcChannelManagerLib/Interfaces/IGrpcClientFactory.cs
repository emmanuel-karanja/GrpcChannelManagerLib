namespace GrpcChannelManagerLib.Interfaces;

public interface IGrpcClientFactory<TClient>
{
    TClient CreateClient(string address);
    TClient CreateClientFromConfiguredEndpoints();
}

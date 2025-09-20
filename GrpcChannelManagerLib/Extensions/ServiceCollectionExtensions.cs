using Microsoft.Extensions.DependencyInjection;
using GrpcChannelManagerLib.Services;
using GrpcChannelManagerLib.Interfaces;
using GrpcChannelManagerLib.Options;
using GrpcChannelManagerLib.Providers;
using Microsoft.Extensions.Logging;

namespace GrpcChannelManagerLib.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGrpcChannelManager(
        this IServiceCollection services,
        Action<GrpcServerOptions>? configureOptions = null)
    {
        if (configureOptions != null)
            services.Configure(configureOptions);

        services.AddSingleton<IGrpcChannelManager, GrpcChannelManager>();
        return services;
    }

    public static IServiceCollection AddGrpcClientFactory<TClient>(
        this IServiceCollection services,
        Func<Grpc.Net.Client.GrpcChannel, TClient> clientCreator)
        where TClient : Grpc.Core.ClientBase<TClient>
    {
        services.AddSingleton<IGrpcClientFactory<TClient>>(sp =>
        {
            var manager = sp.GetRequiredService<IGrpcChannelManager>();
            return new GrpcChannelManagerLib.Services.GrpcClientFactory<TClient>(manager, clientCreator);
        });

        return services;
    }

    public static IServiceCollection AddKeyVaultProvider(this IServiceCollection services, string vaultUri)
    {
        services.AddSingleton<KeyVaultConfigProvider>(sp =>
            new KeyVaultConfigProvider(vaultUri, sp.GetRequiredService<ILogger<KeyVaultConfigProvider>>()));
        return services;
    }

    public static IServiceCollection AddRedisProvider(this IServiceCollection services, string connectionString, string channelName)
    {
        services.AddSingleton<RedisConfigProvider>(sp =>
            new RedisConfigProvider(connectionString, channelName, sp.GetRequiredService<ILogger<RedisConfigProvider>>()));
        return services;
    }
}

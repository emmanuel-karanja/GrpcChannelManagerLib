using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;

namespace GrpcChannelManagerLib.Providers;

public class KeyVaultConfigProvider
{
    private readonly SecretClient _secretClient;
    private readonly ILogger<KeyVaultConfigProvider> _logger;

    public KeyVaultConfigProvider(string vaultUri, ILogger<KeyVaultConfigProvider> logger)
    {
        _logger = logger;
        _secretClient = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());
    }

    /// <summary>
    /// Fetch the list of gRPC endpoints stored in KeyVault as a comma-separated secret.
    /// </summary>
    public async Task<List<string>> GetEndpointsAsync(string secretName, CancellationToken cancellationToken = default)
    {
        try
        {
            KeyVaultSecret secret = await _secretClient.GetSecretAsync(secretName, cancellationToken: cancellationToken);
            if (string.IsNullOrWhiteSpace(secret.Value))
                return new List<string>();

            return secret.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch gRPC endpoints from KeyVault.");
            return new List<string>();
        }
    }
}

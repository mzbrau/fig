using System.ComponentModel;
using Fig.Contracts.SettingClients;
using Fig.Mcp.ApiClient;
using ModelContextProtocol.Server;
using Newtonsoft.Json;

namespace Fig.Mcp.Tools;

[McpServerToolType]
public class ClientManagementTools
{
    [McpServerTool, Description("Rotate a client's authentication secret. " +
        "WARNING: Existing running client instances will continue to use the old secret until oldSecretExpiryUtc, " +
        "after which they MUST be updated with the new secret or they will fail to authenticate. " +
        "Plan a maintenance window or coordinate with teams before changing secrets.")]
    public static async Task<string> ChangeClientSecret(
        IFigApiClient apiClient,
        [Description("The exact name of the Fig client whose secret should be rotated. Use get_client_descriptions to find available client names.")] string clientName,
        [Description("The new secret value to set for the client.")] string newSecret,
        [Description("The UTC date and time when the old secret will stop being accepted. Allows a grace period for clients to update. Example: 2024-02-01T12:00:00Z")] DateTime oldSecretExpiryUtc,
        CancellationToken cancellationToken)
    {
        var request = new ClientSecretChangeRequestDataContract(newSecret, oldSecretExpiryUtc.ToUniversalTime());
        var result = await apiClient.ChangeClientSecretAsync(clientName, request, cancellationToken);
        return JsonConvert.SerializeObject(result, Formatting.Indented);
    }
}

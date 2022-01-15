using System.Net;
using Fig.Contracts.SettingVerification;

namespace Fig.Examples.ConsoleApp;

public class CheckWebsiteVerification : ISettingVerification
{
    private const string WebsiteAddress = "WebsiteAddress";

    public VerificationResultDataContract PerformVerification(Dictionary<string, Object> settingValues)
    {
        var result = new VerificationResultDataContract();
        using HttpClient client = new HttpClient();

        if (!settingValues.ContainsKey(WebsiteAddress))
        {
            result.Message = "No Setting Found";
            return result;
        }

        result.AddLog($"Performing get request to address: {settingValues[WebsiteAddress]}");
        var requestResult = client.GetAsync((string) settingValues[WebsiteAddress]).Result;

        if (requestResult.StatusCode == HttpStatusCode.OK)
        {
            result.Message = "Succeeded";
            result.Success = true;
            return result;
        }
    
        result.AddLog($"Request failed. {requestResult.StatusCode}. {requestResult.ReasonPhrase}");
        result.Message = $"Failed with response: {requestResult.StatusCode}";
        return result;
    }
}
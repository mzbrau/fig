using System.Net;
using Fig.Api.SettingVerification.Sdk;

namespace Fig.Api.SettingVerification.Verifiers;

public class Rest200OkVerifier : ISettingPluginVerifier
{
    public string Name => "Rest200OkVerifier";

    public string Description =>
        "Makes a GET request to the provided endpoint. " +
        "Result is considered success if a status code 200 Ok response is received";
    
    public VerificationResult RunVerification(params object[] parameters)
    {
        if (parameters.Length != 1 || string.IsNullOrEmpty(parameters[0] as string))
        {
            return VerificationResult.IncorrectParameters();
        }

        var result = new VerificationResult();
        var uri = parameters[0] as string;
        
        
        using HttpClient client = new HttpClient();
        
        result.AddLog($"Performing get request to address: {uri}");
        var requestResult = client.GetAsync(uri).Result;

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
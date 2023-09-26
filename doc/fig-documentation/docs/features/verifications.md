---
sidebar_position: 4
---

# Verifications

Fig includes a framework for verifying setting values from within the UI. The setting verifications occur within the Fig API.

Verifications are defined in a dll file placed in a folder called 'plugins' within the base directory of the API. 

The plugin definition must implement in the `ISettingVerifier` interface which is defined in the `Fig.Api.SettingVerification.Sdk` project. Verifiers can recieve the values of more than one setting which are passed in as a list of object params into the verifier.

Many verifications can be defined within the same assembly.

Fig comes with some built in plug in verifications including `PingVerifier` and `Rest200OkVerifier`.

### Usage

```csharp
[Verification("Rest200OkVerifier", nameof(WebsiteAddress))]
public class ProductService : SettingsBase
{
    public override string ClientName => "ProductService";

    [Setting("This is the address of a website", "http://www.google.com")]
    public string WebsiteAddress { get; set; }
}
```

### Example

```csharp
public class Rest200OkVerifier : ISettingVerifier
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
```

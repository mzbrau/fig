---
sidebar_position: 4
---

# Verifications

Fig includes a framework for verifying setting values from within the UI. The setting verifications occur within the Fig API.

There are 2 types of verification: plug in and dynamic.

## Plug In Verifications

Plug in verifications are defined in a dll file placed in a folder called 'plugins' within the base directory of the API. 

The plugin definition must implement in the `ISettingPluginVerifier` interface which is defined in the `Fig.Api.SettingVerification.Sdk` project. Plug in verifiers can recieve the values of more than one setting which are passed in as a list of object params into the verifier.

Many verifications can be defined within the same assembly.

Fig comes with some built in plug in verifications including `PingVerifier` and `Rest200OkVerifier`.

### Usage

```c#
[Verification("Rest200OkVerifier", nameof(WebsiteAddress))]
public class ProductService : SettingsBase
{
    public override string ClientName => "ProductService";

    [Setting("This is the address of a website", "http://www.google.com")]
    public string WebsiteAddress { get; set; }
}
```

### Example

```c#
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
```



## Dynamic Verifications

Dynamic verifications are defined within the setting client. Fig will decompile the code and send it as text to the Fig API. When the verification is requested, fig will recompile the code and execute it. The dynamic verification will receive a dictionary of setting name and setting values to be used for the verification. 

Dynamic verifications must implement the `ISettingVerification` interface which is defined in the `Fig.Client` nuget package.

Dynamic verifications may be seen as a security issue as they technically allow remote code execution. If you do not trust the setting clients accessing fig, dynamic verifications should be disabled in the fig configuration page.

### Usage

The parameters are:

- Name
- Description
- The verifier type
- The target runtime (Note only dotnet 6 has been tested)
- The name of the setting(s) beng verified

```c#
[Verification("WebsiteVerifier", "VerifiesWebsites", typeof(WebsiteVerifier), TargetRuntime.Dotnet6, nameof(WebsiteAddress))]
public class ProductService : SettingsBase
{
    public override string ClientName => "ProductService";

    [Setting("This is the address of a website", "http://www.google.com")]
    public string WebsiteAddress { get; set; }
}
```

### Example

```c#
public class CheckWebsiteVerification : ISettingVerification
{
    private const string WebsiteAddress = "WebsiteAddress";

    public VerificationResultDataContract PerformVerification(IDictionary<string, object?> settingValues)
    {
        var result = new VerificationResultDataContract();
        using HttpClient client = new HttpClient();

        if (!settingValues.ContainsKey(WebsiteAddress))
        {
            result.Message = "No Setting Found";
            return result;
        }

        result.AddLog($"Performing get request to address: {settingValues[WebsiteAddress]}");
        var requestResult = client.GetAsync((string?) settingValues[WebsiteAddress]).Result;

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


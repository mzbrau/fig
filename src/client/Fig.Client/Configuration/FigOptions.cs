using System;

namespace Fig.Client.Configuration;

public class FigOptions : IFigOptions
{
    private const string ApiAddressEnvironmentVariable = "FIG_API_URI";
    private const string FigInstanceEnvironmentVariable = "FIG_{0}_INSTANCE";

    public Uri? ApiUri { get; set; }

    public SecretStore SecretStore { get; set; } = SecretStore.InCode;

    public double PollIntervalMs { get; set; } = 30000;

    public bool LiveReload { get; set; } = true;

    public string? Instance { get; set; }

    public string ClientSecret { get; set; } = string.Empty;

    public string? VersionOverride { get; set; }

    public bool AllowOfflineSettings { get; set; } = true;

    public IFigOptions ReadUriFromEnvironmentVariable()
    {
        var value = Environment.GetEnvironmentVariable(ApiAddressEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"Environment variable {ApiAddressEnvironmentVariable} contained no value");

        ApiUri = new Uri(value);
        return this;
    }
    
    public IFigOptions ReadInstanceFromEnvironmentVariable(string clientName)
    {
        var key = string.Format(FigInstanceEnvironmentVariable, clientName);
        var value = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrWhiteSpace(value))
            Instance = value;
        
        return this;
    }
}
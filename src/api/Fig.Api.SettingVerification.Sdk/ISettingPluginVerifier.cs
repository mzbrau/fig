namespace Fig.Api.SettingVerification.Sdk;

public interface ISettingPluginVerifier
{
    string Name { get; }
    
    string Description { get; }

    VerificationResult RunVerification(params object[] parameters);
}
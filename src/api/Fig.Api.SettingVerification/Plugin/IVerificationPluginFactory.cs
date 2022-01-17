using Fig.Api.SettingVerification.Sdk;

namespace Fig.Api.SettingVerification.Plugin;

public interface IVerificationPluginFactory
{
    ISettingPluginVerifier GetVerifier(string name);
}
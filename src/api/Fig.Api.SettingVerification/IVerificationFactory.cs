using Fig.Api.SettingVerification.Sdk;

namespace Fig.Api.SettingVerification;

public interface IVerificationFactory
{
    ISettingVerifier GetVerifier(string name);

    IEnumerable<string> GetAvailableVerifiers();
}
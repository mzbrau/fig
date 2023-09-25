using Fig.Api.SettingVerification.Exceptions;
using Fig.Api.SettingVerification.Sdk;

namespace Fig.Api.SettingVerification;

public class VerificationFactory : IVerificationFactory
{
    private readonly Dictionary<string, ISettingVerifier> _verifiers;

    public VerificationFactory(IEnumerable<ISettingVerifier> verifiers)
    {
        _verifiers = verifiers.ToDictionary(a => a.Name, b => b);
    }

    public ISettingVerifier GetVerifier(string name)
    {
        if (_verifiers.ContainsKey(name))
            return _verifiers[name];

        throw new UnknownSettingVerifierException(name);
    }

    public IEnumerable<string> GetAvailableVerifiers()
    {
        return _verifiers.Keys;
    }
}
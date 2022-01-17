using Fig.Api.SettingVerification.Exceptions;
using Fig.Api.SettingVerification.Sdk;

namespace Fig.Api.SettingVerification.Plugin;

public class VerificationPluginFactory : IVerificationPluginFactory
{
    private readonly Dictionary<string, ISettingPluginVerifier> _verifiers;

    public VerificationPluginFactory(IEnumerable<ISettingPluginVerifier> verifiers)
    {
        _verifiers = verifiers.ToDictionary(a => a.Name, b => b);
    }
    
    public ISettingPluginVerifier GetVerifier(string name)
    {
        if (_verifiers.ContainsKey(name))
            return _verifiers[name];

        throw new UnknownSettingPluginVerifierException(name);
    }
}
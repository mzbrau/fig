namespace Fig.Api.SettingVerification.Exceptions;

public class UnknownSettingPluginVerifierException : Exception
{
    public UnknownSettingPluginVerifierException(string verifierName)
        : base($"Unknown verifier: {verifierName}")
    {
    }
}
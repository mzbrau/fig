namespace Fig.Api.SettingVerification.Exceptions;

public class UnknownSettingVerifierException : Exception
{
    public UnknownSettingVerifierException(string verifierName)
        : base($"Unknown verifier: {verifierName}")
    {
    }
}
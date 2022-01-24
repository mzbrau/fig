namespace Fig.Api.SettingVerification.Exceptions;

public class CodeTamperedException : Exception
{
    public CodeTamperedException(string verificationName)
    : base($"Code for verification {verificationName} has been tampered with.")
    {
    }
}
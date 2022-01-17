namespace Fig.Api.SettingVerification.Exceptions;

public class InvalidSettingNameException : Exception
{
    public InvalidSettingNameException(string message)
    :base (message)
    {
    }
}
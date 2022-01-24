namespace Fig.Api.Exceptions;

public class InvalidSettingException : Exception
{
    public InvalidSettingException(string message)
        : base(message)
    {
    }
}
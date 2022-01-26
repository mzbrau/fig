namespace Fig.Api.Exceptions;

public class InvalidClientSecretException : Exception
{
    public InvalidClientSecretException(string message)
        : base(message)
    {
    }
}
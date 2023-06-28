namespace Fig.Api.Exceptions;

public class InvalidClientSecretChangeException : Exception
{
    public InvalidClientSecretChangeException(string message) : base(message)
    {
    }
}
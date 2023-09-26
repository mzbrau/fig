namespace Fig.Api.Exceptions;

public class InvalidClientSecretException : Exception
{
    public InvalidClientSecretException()
        : base("Client secret is invalid. It must be at least 32 characters long")
    {
    }
}
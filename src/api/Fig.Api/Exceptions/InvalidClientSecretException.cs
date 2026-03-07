namespace Fig.Api.Exceptions;

public class InvalidClientSecretException : Exception
{
    public InvalidClientSecretException(string? clientSecret)
        : base("Client secret is invalid.")
    {
    }
}
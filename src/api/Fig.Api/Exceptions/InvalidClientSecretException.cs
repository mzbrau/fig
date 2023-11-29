namespace Fig.Api.Exceptions;

public class InvalidClientSecretException : Exception
{
    public InvalidClientSecretException(string? clientSecret)
        : base($"Client secret is invalid. It must be at least 32 characters long " +
               $"but it was {clientSecret?.Length ?? 0} ({clientSecret})")
    {
    }
}
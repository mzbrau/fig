namespace Fig.Api.Exceptions;

public class InvalidClientException : Exception
{
    public InvalidClientException(string clientName) : base($"Unknown client: {clientName}")
    {
    }
}
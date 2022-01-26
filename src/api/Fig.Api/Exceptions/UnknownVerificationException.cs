namespace Fig.Api.Exceptions;

public class UnknownVerificationException : Exception
{
    public UnknownVerificationException(string message)
        : base(message)
    {
    }
}
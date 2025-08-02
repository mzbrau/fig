namespace Fig.Client.Contracts;

public class SecretNotFoundException : Exception
{
    public SecretNotFoundException(string message, Exception? innerException = null) 
        : base(message, innerException)
    {
    }
}
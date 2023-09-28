namespace Fig.Api.Exceptions;

public class InvalidImportException : Exception
{
    public InvalidImportException(string message) 
        : base(message)
    {
    }
}
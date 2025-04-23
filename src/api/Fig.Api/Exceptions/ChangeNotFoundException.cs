namespace Fig.Api.Exceptions;

public class ChangeNotFoundException : Exception
{
    public ChangeNotFoundException(string message)
        : base(message)
    {
    }
}
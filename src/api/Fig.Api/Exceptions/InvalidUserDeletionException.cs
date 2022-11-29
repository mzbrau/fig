namespace Fig.Api.Exceptions;

public class InvalidUserDeletionException : Exception
{
    public InvalidUserDeletionException() : base("At least 1 administrator must exist")
    {
        
    }
}
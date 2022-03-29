namespace Fig.Api.Exceptions;

public class UserExistsException : Exception
{
    public UserExistsException()
        : base("User already exists")
    {
    }
}
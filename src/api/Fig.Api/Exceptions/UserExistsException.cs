namespace Fig.Api.Exceptions;

public class UserExistsException : Exception
{
    public UserExistsException(string username)
        : base($"User {username} already exists")
    {
    }
}
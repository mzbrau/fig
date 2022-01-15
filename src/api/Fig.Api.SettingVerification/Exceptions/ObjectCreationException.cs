namespace Fig.Api.SettingVerification.Exceptions;

public class ObjectCreationException : Exception
{
    public ObjectCreationException(Exception innerException)
    : base("Error creating object", innerException)
    {
    }
}
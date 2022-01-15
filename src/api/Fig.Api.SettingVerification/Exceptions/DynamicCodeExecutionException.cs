namespace Fig.Api.SettingVerification.Exceptions;

public class DynamicCodeExecutionException : Exception
{
    public DynamicCodeExecutionException(Exception innerException)
    : base("Error while executing dynamic code", innerException)
    {
    }
}
namespace Fig.Contracts.Configuration;

public class SecretStoreTestResultDataContract
{
    public SecretStoreTestResultDataContract(bool success, string message)
    {
        Success = success;
        Message = message;
    }

    public bool Success { get; }
    
    public string Message { get; }
}
namespace Fig.WebHooks.TestClient;

public class DataItem
{
    public DataItem(object contract)
    {
        Contract = contract;
        DateTimeAddedUtc = DateTime.UtcNow;
    }

    public DateTime DateTimeAddedUtc { get; }
    
    public object Contract { get; }
}
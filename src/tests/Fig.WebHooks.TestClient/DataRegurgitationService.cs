namespace Fig.WebHooks.TestClient;

public class DataRegurgitationService : IDataRegurgitationService
{
    private readonly List<DataItem> _dataItems = new();
    
    public void Add(object dataContract)
    {
        _dataItems.Add(new DataItem(dataContract));
    }

    public IEnumerable<object> GetAllFromDateTime(DateTime dateTimeUtc)
    {
        return _dataItems.Where(a => a.DateTimeAddedUtc > dateTimeUtc).Select(a => a.Contract);
    }
}
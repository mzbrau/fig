namespace Fig.WebHooks.TestClient;

public interface IDataRegurgitationService
{
    public void Add(object dataContract);

    public IEnumerable<object> GetAllFromDateTime(DateTime dateTimeUtc);
}
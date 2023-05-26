namespace Fig.Web.Models.WebHooks;

public class MatchingClientModel
{
    public MatchingClientModel(string client, string? setting = null)
    {
        Client = client;
        Setting = setting;
    }

    public string Client { get; set; }
    
    public string? Setting { get; set; }
}
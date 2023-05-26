namespace Fig.Web.Models.WebHooks;

public class MatchingClientsModel
{
    public MatchingClientsModel(List<MatchingClientModel> matches)
    {
        Matches = matches;
    }

    public string Summary
    {
        get
        {
            if (!Matches.Any())
                return "Does not match any registered setting clients.";

            var clientsNo = Matches.DistinctBy(a => a.Client).Count();
            var settingsNo = Matches.Count(a => a.Setting is not null);

            return settingsNo == 0 ? 
                $"Matches {clientsNo} setting client(s)" : 
                $"Matches {settingsNo} setting(s) across {clientsNo} setting client(s)";
        }
    }
    
    public List<MatchingClientModel> Matches { get; set; }
}
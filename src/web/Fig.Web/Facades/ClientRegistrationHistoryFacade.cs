using Fig.Common.Events;
using Fig.Contracts.ClientRegistrationHistory;
using Fig.Web.Converters;
using Fig.Web.Events;
using Fig.Web.Models.ClientHistory;
using Fig.Web.Services;

namespace Fig.Web.Facades;

public class ClientRegistrationHistoryFacade : IClientRegistrationHistoryFacade
{
    private readonly IHttpService _httpService;
    private readonly IClientRegistrationHistoryConverter _converter;
    private List<string> _uniqueClientNamesCache = new();
    private bool _uniqueClientNamesDirty = true;

    public ClientRegistrationHistoryFacade(
        IHttpService httpService, 
        IClientRegistrationHistoryConverter converter,
        IEventDistributor eventDistributor)
    {
        _httpService = httpService;
        _converter = converter;
        
        eventDistributor.Subscribe(EventConstants.LogoutEvent, () =>
        {
            Registrations.Clear();
            ImportedDefinitions.Clear();
            MarkUniqueClientNamesDirty();
        });
    }

    public List<ClientRegistrationHistoryModel> Registrations { get; } = new();
    
    public List<string> UniqueClientNames
    {
        get
        {
            if (!_uniqueClientNamesDirty)
                return _uniqueClientNamesCache;

            _uniqueClientNamesCache = Registrations
                .Select(r => r.ClientName)
                .Union(ImportedDefinitions.Select(d => d.ClientName))
                .Distinct()
                .OrderBy(n => n)
                .ToList();

            _uniqueClientNamesDirty = false;
            return _uniqueClientNamesCache;
        }
    }
    
    public List<ImportedClientDefinition> ImportedDefinitions { get; } = new();

    public async Task LoadHistory()
    {
        const string uri = "clientregistrationhistory";
        var result = await _httpService.Get<ClientRegistrationHistoryCollectionDataContract>(uri);

        if (result == null)
            return;

        Registrations.Clear();
        foreach (var registration in result.Registrations.Select(r => _converter.Convert(r)))
        {
            Registrations.Add(registration);
        }

        MarkUniqueClientNamesDirty();
    }

    public List<ClientRegistrationHistoryModel> GetRegistrationsForClient(string clientName)
    {
        return Registrations
            .Where(r => r.ClientName == clientName)
            .OrderBy(r => r.RegistrationDateUtc)
            .ToList();
    }

    public List<ImportedClientDefinition> GetImportedDefinitionsForClient(string clientName)
    {
        return ImportedDefinitions
            .Where(d => d.ClientName == clientName)
            .OrderBy(d => d.GeneratedDateUtc)
            .ToList();
    }

    public void AddImportedDefinition(ImportedClientDefinition definition)
    {
        ImportedDefinitions.Add(definition);
        MarkUniqueClientNamesDirty();
    }

    public void RemoveImportedDefinition(ImportedClientDefinition definition)
    {
        ImportedDefinitions.Remove(definition);
        MarkUniqueClientNamesDirty();
    }

    public void ClearImportedDefinitions()
    {
        ImportedDefinitions.Clear();
        MarkUniqueClientNamesDirty();
    }

    private void MarkUniqueClientNamesDirty()
    {
        _uniqueClientNamesDirty = true;
    }
}

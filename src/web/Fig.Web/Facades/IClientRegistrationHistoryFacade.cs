using Fig.Web.Models.ClientHistory;

namespace Fig.Web.Facades;

public interface IClientRegistrationHistoryFacade
{
    List<ClientRegistrationHistoryModel> Registrations { get; }
    
    List<string> UniqueClientNames { get; }
    
    List<ImportedClientDefinition> ImportedDefinitions { get; }
    
    Task LoadHistory();
    
    List<ClientRegistrationHistoryModel> GetRegistrationsForClient(string clientName);
    
    List<ImportedClientDefinition> GetImportedDefinitionsForClient(string clientName);
    
    void AddImportedDefinition(ImportedClientDefinition definition);
    
    void RemoveImportedDefinition(ImportedClientDefinition definition);
    
    void ClearImportedDefinitions();
}

using System.Collections.Generic;
using System.Threading.Tasks;
using Fig.Contracts.CustomActions;
using Fig.Contracts.LookupTable;

namespace Fig.Client.ConfigurationProvider;

internal interface IFigClientBridge
{
    Task<IEnumerable<CustomActionPollResponseDataContract>?> PollForCustomActionRequests();

    Task SendCustomActionResults(CustomActionExecutionResultsDataContract results);

    Task RegisterCustomActions(List<CustomActionDefinitionDataContract> customActions);

    Task RegisterLookupTable(LookupTableDataContract lookupTable);
}


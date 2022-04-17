using Fig.Contracts.Status;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Converters;

public interface IClientStatusConverter
{
    ClientStatusDataContract Convert(ClientStatusBusinessEntity client);
}
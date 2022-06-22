using Fig.Contracts.Status;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Converters;

public interface IApiStatusConverter
{
    List<ApiStatusDataContract> Convert(IList<ApiStatusBusinessEntity> statuses);
}
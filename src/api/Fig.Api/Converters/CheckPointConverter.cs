using Fig.Contracts.CheckPoint;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Converters;

public class CheckPointConverter : ICheckPointConverter
{
    public CheckPointDataContract Convert(CheckPointBusinessEntity businessEntity)
    {
        return new CheckPointDataContract(
            businessEntity.Id,
            businessEntity.DataId,
            businessEntity.Timestamp,
            businessEntity.NumberOfClients,
            businessEntity.NumberOfSettings,
            businessEntity.AfterEvent,
            businessEntity.Note,
            businessEntity.User);
    }
}
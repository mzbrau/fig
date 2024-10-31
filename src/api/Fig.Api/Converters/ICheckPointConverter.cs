using Fig.Contracts.CheckPoint;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Converters;

public interface ICheckPointConverter
{
    CheckPointDataContract Convert(CheckPointBusinessEntity businessEntity);
}
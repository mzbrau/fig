using Fig.Contracts.Common;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Converters;

public class CommonEnumerationConverter : ICommonEnumerationConverter
{
    public CommonEnumerationBusinessEntity Convert(CommonEnumerationDataContract item)
    {
        return new CommonEnumerationBusinessEntity
        {
            Id = item.Id,
            Name = item.Name,
            Enumeration = item.Enumeration
        };
    }

    public CommonEnumerationDataContract Convert(CommonEnumerationBusinessEntity item)
    {
        return new CommonEnumerationDataContract
        {
            Id =item.Id,
            Name = item.Name,
            Enumeration = item.Enumeration
        };
    }
}
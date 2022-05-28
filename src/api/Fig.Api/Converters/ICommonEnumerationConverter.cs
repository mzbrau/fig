using Fig.Contracts.Common;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Converters;

public interface ICommonEnumerationConverter
{
    CommonEnumerationBusinessEntity Convert(CommonEnumerationDataContract item);

    CommonEnumerationDataContract Convert(CommonEnumerationBusinessEntity item);
}
using Fig.Contracts.Common;
using Fig.Web.Models.CommonEnumerations;

namespace Fig.Web.Converters;

public interface ICommonEnumerationConverter
{
    List<CommonEnumerationModel> Convert(List<CommonEnumerationDataContract> dataContracts);

    CommonEnumerationDataContract Convert(CommonEnumerationModel item);
}
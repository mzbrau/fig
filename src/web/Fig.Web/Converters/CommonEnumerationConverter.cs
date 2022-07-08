using Fig.Contracts.Common;
using Fig.Web.Models.CommonEnumerations;

namespace Fig.Web.Converters;

public class CommonEnumerationConverter : ICommonEnumerationConverter
{
    public List<CommonEnumerationModel> Convert(List<CommonEnumerationDataContract> dataContracts)
    {
        return dataContracts.Select(Convert).ToList();
    }

    public CommonEnumerationDataContract Convert(CommonEnumerationModel item)
    {
        return new CommonEnumerationDataContract(item.Id, item.Name,
            item.Enumerations.ToDictionary(a => a.Key, b => b.Value));
    }

    private CommonEnumerationModel Convert(CommonEnumerationDataContract dataContract)
    {
        return new CommonEnumerationModel
        {
            Id = dataContract.Id,
            Name = dataContract.Name,
            Enumerations = dataContract.Enumeration.Select(a => new CommonEnumerationItemModel
            {
                Key = a.Key,
                Value = a.Value
            }).ToList()
        };
    }
}
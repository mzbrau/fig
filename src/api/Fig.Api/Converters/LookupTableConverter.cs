using Fig.Contracts.Common;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Converters;

public class LookupTableConverter : ILookupTableConverter
{
    public LookupTableBusinessEntity Convert(LookupTableDataContract item)
    {
        return new LookupTableBusinessEntity
        {
            Id = item.Id,
            Name = item.Name,
            LookupTable = item.LookupTable
        };
    }

    public LookupTableDataContract Convert(LookupTableBusinessEntity item)
    {
        return new LookupTableDataContract(item.Id, item.Name, item.LookupTable);
    }
}
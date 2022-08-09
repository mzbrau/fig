using Fig.Contracts.Common;
using Fig.Web.Models.LookupTables;

namespace Fig.Web.Converters;

public class LookupTableConverter : ILookupTableConverter
{
    public List<LookupTables> Convert(List<LookupTableDataContract> dataContracts)
    {
        return dataContracts.Select(Convert).ToList();
    }

    public LookupTableDataContract Convert(LookupTables item)
    {
        return new LookupTableDataContract(item.Id, item.Name,
            item.Lookups.ToDictionary(a => a.Key, b => b.Value));
    }

    private LookupTables Convert(LookupTableDataContract dataContract)
    {
        return new LookupTables
        {
            Id = dataContract.Id,
            Name = dataContract.Name,
            Lookups = dataContract.LookupTable.Select(a => new LookupTablesItemModel
            {
                Key = a.Key,
                Value = a.Value
            }).ToList()
        };
    }
}
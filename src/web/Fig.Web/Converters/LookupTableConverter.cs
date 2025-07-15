using Fig.Contracts.LookupTable;
using Fig.Web.Models.LookupTables;

namespace Fig.Web.Converters;

public class LookupTableConverter : ILookupTableConverter
{
    public List<LookupTable> Convert(List<LookupTableDataContract> dataContracts)
    {
        return dataContracts.Select(Convert).ToList();
    }

    public LookupTableDataContract Convert(LookupTable item)
    {
        return new LookupTableDataContract(item.Id, item.Name,
            item.Lookups.ToDictionary(a => a.Key, b => b.Alias), item.IsClientDefined);
    }

    private LookupTable Convert(LookupTableDataContract dataContract)
    {
        return new LookupTable(id: dataContract.Id, name: dataContract.Name, lookups: dataContract.LookupTable.Select(
            a => new LookupTableItemModel(a.Key, a.Value)).ToList(), dataContract.IsClientDefined);
    }
}
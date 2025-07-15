using Fig.Contracts.LookupTable;
using Fig.Web.Models.LookupTables;

namespace Fig.Web.Converters;

public interface ILookupTableConverter
{
    List<LookupTable> Convert(List<LookupTableDataContract> dataContracts);

    LookupTableDataContract Convert(LookupTable item);
}
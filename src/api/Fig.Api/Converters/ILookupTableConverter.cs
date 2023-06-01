using Fig.Contracts.LookupTable;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Converters;

public interface ILookupTableConverter
{
    LookupTableBusinessEntity Convert(LookupTableDataContract item);

    LookupTableDataContract Convert(LookupTableBusinessEntity item);
}
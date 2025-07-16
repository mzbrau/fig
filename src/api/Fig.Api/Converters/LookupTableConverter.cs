﻿using Fig.Contracts.LookupTable;
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
            LookupTable = item.LookupTable,
            IsClientDefined = item.IsClientDefined
        };
    }

    public LookupTableDataContract Convert(LookupTableBusinessEntity item)
    {
        return new LookupTableDataContract(item.Id, item.Name, item.LookupTable, item.IsClientDefined);
    }
}
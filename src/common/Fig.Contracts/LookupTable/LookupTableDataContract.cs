using System;
using System.Collections.Generic;

namespace Fig.Contracts.LookupTable
{
    public class LookupTableDataContract
    {
        public LookupTableDataContract(Guid? id, string name, Dictionary<string, string?> lookupTable, bool isClientDefined)
        {
            Id = id;
            Name = name;
            LookupTable = lookupTable;
            IsClientDefined = isClientDefined;
        }

        public Guid? Id { get; set; }

        public string Name { get; set; }

        public Dictionary<string, string?> LookupTable { get; set; }
        
        public bool IsClientDefined { get; }
    }
}
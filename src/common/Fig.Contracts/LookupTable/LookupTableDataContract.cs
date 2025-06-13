using System;
using System.Collections.Generic;

namespace Fig.Contracts.LookupTable
{
    public class LookupTableDataContract
    {
        public LookupTableDataContract(Guid? id, string name, Dictionary<string, string?> lookupTable)
        {
            Id = id;
            Name = name;
            LookupTable = lookupTable;
        }

        public Guid? Id { get; set; }

        public string Name { get; set; }

        public Dictionary<string, string?> LookupTable { get; set; }
    }
}
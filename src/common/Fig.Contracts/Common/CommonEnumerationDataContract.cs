using System;
using System.Collections.Generic;

namespace Fig.Contracts.Common
{
    public class CommonEnumerationDataContract
    {
        public Guid Id { get; set; }
        
        public string Name { get; set; }

        public Dictionary<string, string> Enumeration { get; set; }
    }
}
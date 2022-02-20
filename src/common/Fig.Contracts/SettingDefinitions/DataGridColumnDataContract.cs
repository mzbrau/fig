using System;
using System.Collections.Generic;

namespace Fig.Contracts.SettingDefinitions
{
    public class DataGridColumnDataContract
    {
        public DataGridColumnDataContract(string name, Type type)
        {
            Name = name;
            Type = type;
        }
        
        public string Name { get; set; }
    
        public Type Type { get; set; }
        
        public List<string>? ValidValues { get; set; }
    }
}
using System;
using System.Collections.Generic;

namespace Fig.Contracts.SettingDefinitions
{
    public class DataGridColumnDataContract
    {
        public DataGridColumnDataContract(string name, Type type, List<string>? validValues = null)
        {
            Name = name;
            Type = validValues != null ? typeof(string) : type;
            ValidValues = validValues;
        }

        public string Name { get; set; }

        public Type Type { get; set; }

        public List<string>? ValidValues { get; set; }
    }
}
using System;
using System.Collections.Generic;

namespace Fig.Contracts.SettingDefinitions
{
    public class DataGridColumnDataContract
    {
        public DataGridColumnDataContract(string name, Type valueType, List<string>? validValues = null, int? editorLineCount = null)
        {
            Name = name;
            ValueType = validValues != null ? typeof(string) : valueType;
            ValidValues = validValues;
            EditorLineCount = editorLineCount;
        }

        public string Name { get; }

        public Type ValueType { get; }

        public List<string>? ValidValues { get; set; }
        
        public int? EditorLineCount { get; }
    }
}
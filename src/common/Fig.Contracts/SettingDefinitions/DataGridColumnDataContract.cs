using System;
using System.Collections.Generic;

namespace Fig.Contracts.SettingDefinitions
{
    public class DataGridColumnDataContract
    {
        public DataGridColumnDataContract(string name, Type type, List<string>? validValues = null, int? editorLineCount = null)
        {
            Name = name;
            Type = validValues != null ? typeof(string) : type;
            ValidValues = validValues;
            EditorLineCount = editorLineCount;
        }

        public string Name { get; }

        public Type Type { get; }

        public List<string>? ValidValues { get; }
        
        public int? EditorLineCount { get; }
    }
}
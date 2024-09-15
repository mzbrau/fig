using System;
using System.Collections.Generic;

namespace Fig.Contracts.SettingDefinitions
{
    public class DataGridColumnDataContract
    {
        public DataGridColumnDataContract(string name,
            Type valueType,
            List<string>? validValues = null,
            int? editorLineCount = null,
            bool isReadOnly = false,
            string? validationRegex = null,
            string? validationExplanation = null,
            bool isSecret = false)
        {
            Name = name;
            ValueType = validValues != null && valueType != typeof(List<string>) ? typeof(string) : valueType;
            ValidValues = validValues;
            EditorLineCount = editorLineCount;
            IsReadOnly = isReadOnly;
            ValidationRegex = validationRegex;
            ValidationExplanation = validationExplanation;
            IsSecret = isSecret;
        }

        public string Name { get; }

        public Type ValueType { get; }

        public List<string>? ValidValues { get; set; }
        
        public string? ValidationRegex { get; set; }
        
        public string? ValidationExplanation { get; set; }
        
        public int? EditorLineCount { get; }

        public bool IsReadOnly { get; set; }
        
        public bool IsSecret { get; set; }
    }
}
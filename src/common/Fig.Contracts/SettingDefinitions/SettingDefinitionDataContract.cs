using System;
using System.Collections.Generic;
using Fig.Contracts.Settings;

namespace Fig.Contracts.SettingDefinitions
{
    public class SettingDefinitionDataContract
    {
        public SettingDefinitionDataContract(string name,
            string description, 
            SettingValueBaseDataContract? value = null,
            bool isSecret = false,
            Type? valueType = null,
            SettingValueBaseDataContract? defaultValue = null,
            ValidationType validationType = ValidationType.None,
            string? validationRegex = null,
            string? validationExplanation = null,
            List<string>? validValues = null,
            string? @group = null,
            int? displayOrder = null,
            bool advanced = false,
            string? lookupTableKey = null,
            int? editorLineCount = null,
            string? jsonSchema = null,
            DataGridDefinitionDataContract? dataGridDefinition = null,
            IList<string>? enablesSettings = null,
            bool supportsLiveUpdate = true,
            DateTime? lastChanged = null)
        {
            Name = name;
            Description = description;
            EnablesSettings = enablesSettings;
            IsSecret = isSecret;
            ValueType = valueType;
            Value = value;
            DefaultValue = defaultValue;
            ValidationType = validationType;
            ValidationRegex = validationRegex;
            ValidationExplanation = validationExplanation;
            ValidValues = validValues;
            Group = @group;
            DisplayOrder = displayOrder;
            Advanced = advanced;
            LookupTableKey = lookupTableKey;
            EditorLineCount = editorLineCount;
            JsonSchema = jsonSchema;
            DataGridDefinition = dataGridDefinition;
            SupportsLiveUpdate = supportsLiveUpdate;
            LastChanged = lastChanged;
        }

        public string Name { get; }

        public string Description { get;  set; }

        public bool IsSecret { get; set; }
        
        public Type? ValueType { get; set; }
        
        public SettingValueBaseDataContract? Value { get; set; }
        
        public SettingValueBaseDataContract? DefaultValue { get; set; }

        public ValidationType ValidationType { get; set; }

        public string? ValidationRegex { get; set; }

        public string? ValidationExplanation { get; set; }

        public List<string>? ValidValues { get; set; }

        public string? Group { get; set; }

        public int? DisplayOrder { get; set; }

        public bool Advanced { get; set; }

        public string? LookupTableKey { get; set; }

        public int? EditorLineCount { get; set; }

        public string? JsonSchema { get; set; }

        public DataGridDefinitionDataContract? DataGridDefinition { get; set; }
        
        public IList<string>? EnablesSettings { get; set; }
        
        public bool SupportsLiveUpdate { get; set; }
        
        public DateTime? LastChanged { get; set; }
    }
}
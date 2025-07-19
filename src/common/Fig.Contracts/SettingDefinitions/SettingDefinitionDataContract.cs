using System;
using System.Collections.Generic;
using Fig.Common.NetStandard.Data;
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
            DateTime? lastChanged = null,
            string? categoryColor = null,
            string? categoryName = null,
            string? displayScript = null,
            bool isExternallyManaged = false,
            Classification classification = Classification.Technical,
            bool? environmentSpecific = null,
            string? lookupKeySettingName = null,
            int? indent = null,
            string? dependsOnProperty = null,
            IList<string>? dependsOnValidValues = null)
        {
            Name = name;
            Description = description;
            EnablesSettings = enablesSettings;
            IsSecret = isSecret;
            ValueType = valueType;
            Value = value;
            DefaultValue = defaultValue;
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
            CategoryColor = categoryColor;
            CategoryName = categoryName;
            DisplayScript = displayScript;
            IsExternallyManaged = isExternallyManaged;
            Classification = classification;
            EnvironmentSpecific = environmentSpecific;
            LookupKeySettingName = lookupKeySettingName;
            Indent = indent;
            DependsOnProperty = dependsOnProperty;
            DependsOnValidValues = dependsOnValidValues;
        }

        public string Name { get; }

        public string Description { get;  set; }

        public bool IsSecret { get; set; }
        
        public Type? ValueType { get; set; }
        
        public SettingValueBaseDataContract? Value { get; set; }
        
        public SettingValueBaseDataContract? DefaultValue { get; set; }

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
        
        public string? CategoryName { get; set; }
        
        public string? CategoryColor { get; set; }
        
        public string? DisplayScript { get; set; }
        
        public bool IsExternallyManaged { get; set; }
        
        public Classification Classification { get; set; }
        
        public bool? EnvironmentSpecific { get; set; }
        
        public string? LookupKeySettingName { get; set; }
        
        public int? Indent { get; set; }
        
        public string? DependsOnProperty { get; set; }
        
        public IList<string>? DependsOnValidValues { get; set; }
    }
}
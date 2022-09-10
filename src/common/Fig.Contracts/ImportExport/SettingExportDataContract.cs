using System;
using System.Collections.Generic;
using Fig.Contracts.JsonConversion;
using Newtonsoft.Json;

namespace Fig.Contracts.ImportExport
{
    public class SettingExportDataContract
    {
        public SettingExportDataContract(string name,
            string description,
            bool isSecret,
            Type valueType,
            dynamic? value,
            dynamic? defaultValue,
            bool isEncrypted,
            string? jsonSchema,
            string validationType,
            string? validationRegex,
            string? validationExplanation,
            IList<string>? validValues,
            string? @group,
            int? displayOrder,
            bool advanced,
            string? lookupTableKey,
            int? editorLineCount,
            string? dataGridDefinitionJson,
            IList<string>? enablesSettings,
            bool supportsLiveUpdate)
        {
            Name = name;
            Description = description;
            IsSecret = isSecret;
            ValueType = valueType;
            Value = value;
            DefaultValue = defaultValue;
            IsEncrypted = isEncrypted;
            JsonSchema = jsonSchema;
            ValidationType = validationType;
            ValidationRegex = validationRegex;
            ValidationExplanation = validationExplanation;
            ValidValues = validValues;
            Group = @group;
            DisplayOrder = displayOrder;
            Advanced = advanced;
            LookupTableKey = lookupTableKey;
            EditorLineCount = editorLineCount;
            DataGridDefinitionJson = dataGridDefinitionJson;
            EnablesSettings = enablesSettings;
            SupportsLiveUpdate = supportsLiveUpdate;
        }

        public string Name { get; }

        public string Description { get; }

        public bool IsSecret { get; }

        public Type ValueType { get; }

        [JsonConverter(typeof(DynamicObjectConverter))]
        public dynamic? Value { get; }

        [JsonConverter(typeof(DynamicObjectConverter))]
        public dynamic? DefaultValue { get; }

        public bool IsEncrypted { get; }

        public string? JsonSchema { get; }

        public string ValidationType { get; }

        public string? ValidationRegex { get; }

        public string? ValidationExplanation { get; }

        public IList<string>? ValidValues { get; }

        public string? Group { get; }

        public int? DisplayOrder { get; }

        public bool Advanced { get; }

        public string? LookupTableKey { get; }

        public int? EditorLineCount { get; }

        public string? DataGridDefinitionJson { get; }
        
        public IList<string>? EnablesSettings { get; }
        
        public bool SupportsLiveUpdate { get; set; }
    }
}
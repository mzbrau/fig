using System;
using System.Collections.Generic;
using Fig.Contracts.JsonConversion;
using Newtonsoft.Json;

namespace Fig.Contracts.SettingDefinitions
{
    public class SettingDefinitionDataContract
    {
        public SettingDefinitionDataContract(string name,
            string description,
            bool isSecret = false,
            dynamic? value = null,
            dynamic? defaultValue = null,
            Type? valueType = null,
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
            DataGridDefinitionDataContract? dataGridDefinition = null)
        {
            Name = name;
            Description = description;
            IsSecret = isSecret;
            Value = value;
            DefaultValue = defaultValue;
            ValueType = valueType;
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
        }

        public string Name { get; }

        public string Description { get;  set; }

        public bool IsSecret { get; set; }

        [JsonConverter(typeof(DynamicObjectConverter))]
        public dynamic? Value { get; set; }

        [JsonConverter(typeof(DynamicObjectConverter))]
        public dynamic? DefaultValue { get; set; }

        public Type? ValueType { get; set; }

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
    }
}
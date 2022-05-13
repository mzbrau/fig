using System;
using System.Collections.Generic;
using Fig.Contracts.JsonConversion;
using Newtonsoft.Json;

namespace Fig.Contracts.ImportExport
{
    public class SettingExportDataContract
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public bool IsSecret { get; set; }

        public Type ValueType { get; set; }

        [JsonConverter(typeof(DynamicObjectConverter))]
        public dynamic? Value { get; set; }

        [JsonConverter(typeof(DynamicObjectConverter))]
        public dynamic? DefaultValue { get; set; }

        public string? EncryptionCertificateThumbprint { get; set; }

        public string? JsonSchema { get; set; }

        public string ValidationType { get; set; }

        public string? ValidationRegex { get; set; }

        public string? ValidationExplanation { get; set; }

        public IList<string>? ValidValues { get; set; }

        public string? Group { get; set; }

        public int? DisplayOrder { get; set; }

        public bool Advanced { get; set; }

        public string? StringFormat { get; set; }

        public int? EditorLineCount { get; set; }

        public string? DataGridDefinitionJson { get; set; }
    }
}
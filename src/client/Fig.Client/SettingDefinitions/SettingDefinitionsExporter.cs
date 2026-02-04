using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fig.Contracts.SettingDefinitions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Fig.Client.SettingDefinitions;

internal class SettingDefinitionsExporter
{
    public void Export(SettingsClientDefinitionDataContract definition)
    {
        var generatedDateUtc = DateTime.UtcNow;
        var exportModel = new ExportedSettingDefinitions
        {
            ClientName = definition.Name,
            ClientVersion = definition.ClientVersion ?? string.Empty,
            GeneratedDateUtc = generatedDateUtc,
            Settings = definition.Settings.Select(s => new ExportedSettingDefaultValue
            {
                Name = s.Name,
                DefaultValue = s.DefaultValue?.GetValue()?.ToString(),
                Advanced = s.Advanced
            }).ToList()
        };

        var settings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.Indented
        };

        var json = JsonConvert.SerializeObject(exportModel, settings);
        var fileName = GetSafeFileName(definition.Name, generatedDateUtc);
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
        
        File.WriteAllText(filePath, json);
        
        Console.WriteLine($"Setting definitions exported to: {filePath}");
    }

    private string GetSafeFileName(string clientName, DateTime generatedDateUtc)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var safeName = new string(clientName.Where(c => !invalidChars.Contains(c)).ToArray());
        var timestamp = generatedDateUtc.ToString("yyyyMMdd_HHmmss");
        return $"{safeName}_{timestamp}.json";
    }
}

internal class ExportedSettingDefinitions
{
    public string ClientName { get; set; } = string.Empty;
    
    public string ClientVersion { get; set; } = string.Empty;
    
    public DateTime GeneratedDateUtc { get; set; }
    
    public List<ExportedSettingDefaultValue> Settings { get; set; } = new();
}

internal class ExportedSettingDefaultValue
{
    public string Name { get; set; } = string.Empty;
    
    public string? DefaultValue { get; set; }

    public bool Advanced { get; set; }
}

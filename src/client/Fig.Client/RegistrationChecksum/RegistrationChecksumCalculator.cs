using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Fig.Common.NetStandard.Json;
using Fig.Contracts.SettingDefinitions;
using Newtonsoft.Json;

namespace Fig.Client.RegistrationChecksum;

public static class RegistrationChecksumCalculator
{
    public static string Compute(SettingsClientDefinitionDataContract definition)
    {
        var payload = new RegistrationChecksumPayload(
            definition.Description,
            definition.HasDisplayScripts,
            definition.Settings
                .OrderBy(s => s.DisplayOrder)
                .ThenBy(s => s.Name, StringComparer.Ordinal)
                .ToList(),
            definition.CustomActions
                .OrderBy(a => a.Name, StringComparer.Ordinal)
                .ToList());
        var json = JsonConvert.SerializeObject(payload, JsonSettings.FigDefault);
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
        return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
    }
}

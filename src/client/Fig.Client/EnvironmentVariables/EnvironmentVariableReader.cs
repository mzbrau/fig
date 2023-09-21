using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fig.Contracts;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Microsoft.Extensions.Logging;

namespace Fig.Client.EnvironmentVariables;

public class EnvironmentVariableReader : IEnvironmentVariableReader
{
    public IEnumerable<SettingDataContract> ReadSettingOverrides(IList<SettingDefinitionDataContract> settings)
    {
        var result = new List<SettingDataContract>();
        var allEnvironmentVariables = Environment.GetEnvironmentVariables();

        foreach (DictionaryEntry variable in allEnvironmentVariables)
        {
            var match = settings.FirstOrDefault(a => a.Name == variable.Key.ToString());
            if (match is not null)
            {
                result.Add(new SettingDataContract(
                    match.Name,
                    ValueDataContractFactory.CreateContract(variable.Value, match.ValueType!)));
            }
        }

        return result;
    }
}
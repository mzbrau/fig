using System;
using System.Collections.Generic;

namespace Fig.Contracts.SettingVerification;

[Obsolete("Removed in Fig 2.0")]
public class SettingVerificationDefinitionDataContract
{
    public SettingVerificationDefinitionDataContract(string name, string description,
        List<string> propertyArguments)
    {
        Name = name;
        Description = description;
        PropertyArguments = propertyArguments;
    }

    public string Name { get; }

    public string Description { get; }

    public List<string> PropertyArguments { get; }
}
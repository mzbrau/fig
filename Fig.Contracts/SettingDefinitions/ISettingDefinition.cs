using System;
namespace Fig.Contracts.SettingDefinitions
{
    public interface ISettingDefinition
    {
        string Name { get; set; }

        bool IsSecret { get; set; }

        object DefaultValue { get; set; }

        string ValidationRegex { get; set; }

        string Description { get; set; }

        string ValidationExplanation { get; set; }

        string Group { get; set; }
    }
}


using System;
using Fig.Contracts.SettingTypes;

namespace Fig.Contracts.SettingDefinitions
{
    public class SettingDefinitionDataContract<T> where T : SettingType
    {
        public string Name { get; set; }

        public bool IsSecret { get; set; }

        public T DefaultValue { get; set; }

        public string ValidationRegex { get; set; }

        public string Description { get; set; }

        public string ValidationExplanation { get; set; }

        public string Group { get; set; }
    }
}


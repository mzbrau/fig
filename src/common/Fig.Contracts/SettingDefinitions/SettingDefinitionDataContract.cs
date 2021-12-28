using System;
using Fig.Contracts.SettingTypes;

namespace Fig.Contracts.SettingDefinitions
{
    public class SettingDefinitionDataContract<T>: ISettingDefinition where T : SettingType
    {
        public string Name { get; set; }

        public bool IsSecret { get; set; }

        public T TypedDefaultValue { get; set; }

        public object DefaultValue
        {
            get => TypedDefaultValue?.Value;
            set => TypedDefaultValue = (T)Activator.CreateInstance(typeof(T), value);
        }

        public string ValidationRegex { get; set; }

        public string Description { get; set; }

        public string ValidationExplanation { get; set; }

        public string Group { get; set; }
        
        public string FriendlyName { get; set; }
    }
}


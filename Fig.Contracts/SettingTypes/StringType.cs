using System;
using Fig.Contracts.SettingDefinitions;

namespace Fig.Contracts.SettingTypes
{
    public class StringType : SettingType
    {
        public StringType(string value)
        {
            Value = value;
        }

        public sealed override object Value { get; set; }
    }
}


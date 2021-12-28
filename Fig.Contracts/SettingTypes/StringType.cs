using System;
namespace Fig.Contracts.SettingTypes
{
    public class StringType : SettingType
    {
        public StringType(string value)
        {
        }

        public string Value { get; set; }
    }
}


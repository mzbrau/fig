using System;

namespace Fig.Client.Attributes
{
    public class SettingStringFormatAttribute : Attribute
    {
        public SettingStringFormatAttribute(string stringFormat)
        {
            StringFormat = stringFormat;
        }
        
        public string StringFormat { get; }
    }
}
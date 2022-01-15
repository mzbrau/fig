using System;

namespace Fig.Client.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class SettingStringFormatAttribute : Attribute
    {
        public SettingStringFormatAttribute(string stringFormat)
        {
            StringFormat = stringFormat;
        }
        
        public string StringFormat { get; }
    }
}
using System;
namespace Fig.Client.Attributes
{
    public class SettingAttribute : Attribute
    {
        public SettingAttribute(string description, object defaultValue)
        {
            Description = description;
            DefaultValue = defaultValue;
        }
        
        public string Description { get; }
        
        public object DefaultValue { get; }
    }
}


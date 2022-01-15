using System;
namespace Fig.Client.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class SettingAttribute : Attribute
    {
        public SettingAttribute(string description, object defaultValue = null)
        {
            Description = description;
            DefaultValue = defaultValue;
        }
        
        public string Description { get; }
        
        public object DefaultValue { get; }
    }
}


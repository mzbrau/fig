using System;
namespace Fig.Client.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class SettingAttribute : Attribute
    {
        public SettingAttribute(string description, object? defaultValue = null, bool supportsLiveUpdate = true)
        {
            Description = description;
            DefaultValue = defaultValue;
            SupportsLiveUpdate = supportsLiveUpdate;
        }
        
        public string Description { get; }
        
        public object? DefaultValue { get; }
        
        public bool SupportsLiveUpdate { get; }
    }
}


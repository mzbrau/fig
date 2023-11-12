using System;
namespace Fig.Client.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class SettingAttribute : Attribute
    {
        public SettingAttribute(string description,
            bool supportsLiveUpdate = true)
        {
            Description = description;
            SupportsLiveUpdate = supportsLiveUpdate;
        }
        
        public string Description { get; }
        
        public bool SupportsLiveUpdate { get; }
    }
}


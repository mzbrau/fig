using System;

namespace Fig.Client.Attributes
{
    public class SettingDescriptionAttribute : Attribute
    {
        public SettingDescriptionAttribute(string description)
        {
            Description = description;
        }
        
        public string Description { get; }
    }
}
using System;

namespace Fig.Client.Attributes
{
    public class DescriptionAttribute : Attribute
    {
        public DescriptionAttribute(string description)
        {
            Description = description;
        }
        
        public string Description { get; }
    }
}
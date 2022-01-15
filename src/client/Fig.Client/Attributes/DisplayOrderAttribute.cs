using System;

namespace Fig.Client.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DisplayOrderAttribute : Attribute
    {
        public DisplayOrderAttribute(int displayOrder)
        {
            DisplayOrder = displayOrder;
        }
        
        public int DisplayOrder { get; }
    }
}
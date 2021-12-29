using System;

namespace Fig.Client.Attributes
{
    public class DisplayOrderAttribute : Attribute
    {
        public DisplayOrderAttribute(int displayOrder)
        {
            DisplayOrder = displayOrder;
        }
        
        public int DisplayOrder { get; }
    }
}
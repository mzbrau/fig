using System;

namespace Fig.Client.Attributes
{
    public class MultiLineAttribute : Attribute
    {
        public MultiLineAttribute(int numberOfLines)
        {
            NumberOfLines = numberOfLines;
        }
        
        public int NumberOfLines { get; }
    }
}
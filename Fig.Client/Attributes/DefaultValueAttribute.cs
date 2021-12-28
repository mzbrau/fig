using System;

namespace Fig.Client.Attributes
{
    public class DefaultValueAttribute : Attribute
    {
        public DefaultValueAttribute(object value)
        {
            Value = value;
        }
        
        public object Value { get; }
    }
}
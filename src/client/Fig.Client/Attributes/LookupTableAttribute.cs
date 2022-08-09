using System;

namespace Fig.Client.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class LookupTableAttribute : Attribute
    {
        public LookupTableAttribute(string lookupTableKey)
        {
            LookupTableKey = lookupTableKey;
        }
        
        public string LookupTableKey { get; }
    }
}
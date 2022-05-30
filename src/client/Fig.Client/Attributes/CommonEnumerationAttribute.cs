using System;

namespace Fig.Client.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class CommonEnumerationAttribute : Attribute
    {
        public CommonEnumerationAttribute(string commonEnumerationKey)
        {
            CommonEnumerationKey = commonEnumerationKey;
        }
        
        public string CommonEnumerationKey { get; }
    }
}
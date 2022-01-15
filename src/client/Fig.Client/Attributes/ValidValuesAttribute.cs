using System;
using System.Collections.Generic;
using System.Linq;

namespace Fig.Client.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ValidValuesAttribute : Attribute
    {
        public ValidValuesAttribute(Type enumType)
        {
            Values = Enum.GetNames(enumType);
        }
        
        public ValidValuesAttribute(params string[] values)
        {
            Values = values;
        }
        
        public string[] Values { get; }
    }
}
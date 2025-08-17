using System;

namespace Fig.Client.Abstractions.Attributes
{
    public class ValidationDefinitionAttribute : Attribute
    {
        public ValidationDefinitionAttribute(string regex, string explanation)
        {
            Regex = regex;
            Explanation = explanation;
        }
        
        public string Regex { get; }
        
        public string Explanation { get; }
    }
}
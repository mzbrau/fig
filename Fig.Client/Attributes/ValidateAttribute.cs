using System;

namespace Fig.Client.Attributes
{
    public class ValidateAttribute : Attribute
    {
        public ValidateAttribute(string validationRegex, string explanation)
        {
            ValidationRegex = validationRegex;
            Explanation = explanation;
        }
        
        public string ValidationRegex { get; }
        
        public string Explanation { get; }
    }
}
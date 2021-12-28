using System;

namespace Fig.Client.Attributes
{
    public class ValidationAttribute : Attribute
    {
        public ValidationAttribute(string validationRegex, string explanation)
        {
            ValidationRegex = validationRegex;
            Explanation = explanation;
        }
        
        public string ValidationRegex { get; }
        
        public string Explanation { get; }
    }
}
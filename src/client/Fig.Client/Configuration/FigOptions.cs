using System;

namespace Fig.Client.Configuration
{
    public class FigOptions : IFigOptions
    {
        public Uri ApiUri { get; private set; }
        
        public FigOptions StaticUri(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException();
            }
            
            ApiUri = new Uri(value);
            return this;
        }

        public FigOptions ReadUriFromEnvironmentVariable(string variableName)
        {
            var value = Environment.GetEnvironmentVariable(variableName);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"Environment variable {variableName} contained no value");
            }
            
            ApiUri = new Uri(value);
            return this;
        }
    }
}
namespace Fig.Client.Attributes
{
    public class StringFormatAttribute
    {
        public StringFormatAttribute(string stringFormat)
        {
            StringFormat = stringFormat;
        }
        
        public string StringFormat { get; }
    }
}
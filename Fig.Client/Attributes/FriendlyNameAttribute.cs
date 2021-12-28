using System;

namespace Fig.Client.Attributes
{
    public class FriendlyNameAttribute : Attribute
    {
        public FriendlyNameAttribute(string friendlyName)
        {
            FriendlyName = friendlyName;
        }
        
        public string FriendlyName { get; }
    }
}
using System;

namespace Fig.Client.Exceptions
{
    public class InvalidSettingException : Exception
    {
        public InvalidSettingException(string message) 
            : base(message)
        {
        }
    }
}
using System;

namespace Fig.Client.Exceptions
{
    public class InvalidSettingVerificationException : Exception
    {
        public InvalidSettingVerificationException(string message)
            : base(message)
        {
        }
    }
}
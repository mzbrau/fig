using System;

namespace Fig.Client.Exceptions
{
    public class InvalidDefaultValueException : Exception
    {
        public InvalidDefaultValueException(string message)
            : base(message)
        {
        }
    }
}
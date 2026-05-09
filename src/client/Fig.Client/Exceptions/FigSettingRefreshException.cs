using System;

namespace Fig.Client.Exceptions;

public class FigSettingRefreshException : Exception
{
    public FigSettingRefreshException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

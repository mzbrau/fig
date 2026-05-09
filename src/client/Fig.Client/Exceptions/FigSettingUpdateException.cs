using System;
using Fig.Contracts;

namespace Fig.Client.Exceptions;

public class FigSettingUpdateException : Exception
{
    public FigSettingUpdateException(ErrorResultDataContract? result)
        : base(result?.Message)
    {
        Result = result;
    }

    public ErrorResultDataContract? Result { get; }
}

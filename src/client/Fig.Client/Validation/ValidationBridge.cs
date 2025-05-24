using System;
using System.Collections.Generic;

namespace Fig.Client.Validation;

public static class ValidationBridge
{
    public static Func<IEnumerable<string>>? GetConfigurationErrors;
}
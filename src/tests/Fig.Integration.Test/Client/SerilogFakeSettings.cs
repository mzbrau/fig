using System.Collections.Generic;
using Fig.Test.Common.TestSettings;

namespace Fig.Integration.Test.Client;

public class SerilogFakeSettings
{
    public Override? Override { get; set; }
    
    public string? Stuff { get; set; }
}

public class Override
{
    public string? Microsoft { get; set; }
    
    public string? Google { get; set; }
    
    public string? Amazon { get; set; }
    
    public Value? Value { get; set; }
}

public class Value
{
    public string? Google { get; set; }
}
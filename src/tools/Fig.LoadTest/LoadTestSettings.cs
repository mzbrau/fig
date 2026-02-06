using System.Collections.Generic;
using Fig.Client;
using Fig.Client.Abstractions.Attributes;

namespace Fig.LoadTest;

public sealed class LoadTestSettings01 : SettingsBase
{
    public override string ClientDescription => "Load test settings for client 01.";

    [Setting("Sample setting for client 01")]
    public int Value01 { get; set; } = 1;

    public override IEnumerable<string> GetValidationErrors() => [];
}

public sealed class LoadTestSettings02 : SettingsBase
{
    public override string ClientDescription => "Load test settings for client 02.";

    [Setting("Sample setting for client 02")]
    public int Value02 { get; set; } = 2;

    public override IEnumerable<string> GetValidationErrors() => [];
}

public sealed class LoadTestSettings03 : SettingsBase
{
    public override string ClientDescription => "Load test settings for client 03.";

    [Setting("Sample setting for client 03")]
    public int Value03 { get; set; } = 3;

    public override IEnumerable<string> GetValidationErrors() => [];
}

public sealed class LoadTestSettings04 : SettingsBase
{
    public override string ClientDescription => "Load test settings for client 04.";

    [Setting("Sample setting for client 04")]
    public int Value04 { get; set; } = 4;

    public override IEnumerable<string> GetValidationErrors() => [];
}

public sealed class LoadTestSettings05 : SettingsBase
{
    public override string ClientDescription => "Load test settings for client 05.";

    [Setting("Sample setting for client 05")]
    public int Value05 { get; set; } = 5;

    public override IEnumerable<string> GetValidationErrors() => [];
}

public sealed class LoadTestSettings06 : SettingsBase
{
    public override string ClientDescription => "Load test settings for client 06.";

    [Setting("Sample setting for client 06")]
    public int Value06 { get; set; } = 6;

    public override IEnumerable<string> GetValidationErrors() => [];
}

public sealed class LoadTestSettings07 : SettingsBase
{
    public override string ClientDescription => "Load test settings for client 07.";

    [Setting("Sample setting for client 07")]
    public int Value07 { get; set; } = 7;

    public override IEnumerable<string> GetValidationErrors() => [];
}

public sealed class LoadTestSettings08 : SettingsBase
{
    public override string ClientDescription => "Load test settings for client 08.";

    [Setting("Sample setting for client 08")]
    public int Value08 { get; set; } = 8;

    public override IEnumerable<string> GetValidationErrors() => [];
}

public sealed class LoadTestSettings09 : SettingsBase
{
    public override string ClientDescription => "Load test settings for client 09.";

    [Setting("Sample setting for client 09")]
    public int Value09 { get; set; } = 9;

    public override IEnumerable<string> GetValidationErrors() => [];
}

public sealed class LoadTestSettings10 : SettingsBase
{
    public override string ClientDescription => "Load test settings for client 10.";

    [Setting("Sample setting for client 10")]
    public int Value10 { get; set; } = 10;

    public override IEnumerable<string> GetValidationErrors() => [];
}

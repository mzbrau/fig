using Fig.Client.Abstractions.Attributes;

namespace Fig.Test.Common.TestSettings;

/// <summary>
/// Test settings class for validating environment variable override functionality.
/// Includes simple types, lists, and complex objects.
/// </summary>
public class EnvironmentVariableOverrideSettings : TestSettingsBase
{
    public override string ClientName => "EnvironmentVariableOverrideSettings";
    public override string ClientDescription => "Settings for testing environment variable overrides";

    [Setting("A string setting")]
    public string StringSetting { get; set; } = "OriginalString";

    [Setting("An int setting")]
    public int IntSetting { get; set; } = 42;

    [Setting("A bool setting")]
    public bool BoolSetting { get; set; } = false;

    [Setting("A double setting")]
    public double DoubleSetting { get; set; } = 3.14;

    [Setting("A list of strings")]
    public List<string>? StringList { get; set; }

    [Setting("A list of complex objects", defaultValueMethodName: nameof(GetDefaultComplexList))]
    public List<ComplexItem>? ComplexList { get; set; }

    public override IEnumerable<string> GetValidationErrors()
    {
        return [];
    }

    public static List<ComplexItem> GetDefaultComplexList()
    {
        return new List<ComplexItem>
        {
            new() { StringVal = "Default1", IntVal = 1 },
            new() { StringVal = "Default2", IntVal = 2 }
        };
    }
}

/// <summary>
/// Complex item class for testing environment variable overrides on complex objects.
/// </summary>
public class ComplexItem
{
    public string StringVal { get; set; } = string.Empty;
    public int IntVal { get; set; }

    public override bool Equals(object? obj)
    {
        var other = obj as ComplexItem;
        return $"{StringVal}-{IntVal}" == $"{other?.StringVal}-{other?.IntVal}";
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(StringVal, IntVal);
    }
}

using Fig.Client.Abstractions.Attributes;
using Fig.Client.Abstractions.Enums;

namespace Fig.Test.Common.TestSettings;

public class HeadingConstructorTestSettings : TestSettingsBase
{
    public override string ClientName => "HeadingConstructorTestSettings";
    public override string ClientDescription => "Test settings for Heading attribute constructor variations";

    // Test original constructor with manual text and color
    [Heading("Manual Configuration", color: "#FF5733")]
    [Setting("Manual Setting 1")]
    public string ManualSetting1 { get; set; } = "Value1";

    // Test constructor with predefined CategoryColor
    [Heading("Color Enum Section", CategoryColor.Blue)]
    [Setting("Color Enum Setting")]
    public string ColorEnumSetting { get; set; } = "Value2";

    // Test constructor with predefined Category (uses category name and color)
    [Heading(Category.Database)]
    [Setting("Database Connection String")]
    public string DatabaseConnection { get; set; } = "Server=localhost;Database=Test";

    // Test with CategoryColor
    [Heading("Color Enum Section", CategoryColor.Green)]
    [Setting("Indented Setting")]
    public string IndentedSetting { get; set; } = "Value3";

    // Test with Category
    [Heading(Category.Authentication)]
    [Setting("Auth Token")]
    public string AuthToken { get; set; } = "token123";

    // Test multiple categories for grouping
    [Heading(Category.Logging)]
    [Setting("Log Level")]
    public string LogLevel { get; set; } = "Info";

    [Heading(Category.Security)]
    [Setting("Encryption Key")]
    [Secret]
    public string? EncryptionKey { get; set; }

    // Test with original constructor for comparison
    [Heading("Custom Section", color: "#8A2BE2")]
    [Setting("Custom Setting")]
    public string CustomSetting { get; set; } = "CustomValue";

    public override IEnumerable<string> GetValidationErrors()
    {
        return [];
    }
}

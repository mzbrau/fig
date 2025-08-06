---
sidebar_position: 7
---

# Display Script Testing

Fig provides a comprehensive testing framework that allows you to test your display scripts offline, without needing to run the full Fig web application. This is particularly useful for automated testing, CI/CD pipelines, and rapid development iterations.

:::note Related Testing
This guide covers testing Fig display scripts (JavaScript logic). If you're looking to test ASP.NET Core applications that use Fig for configuration, see the [Integration Testing](./4-integration-testing.md) guide which covers testing your application with Fig configuration providers.
:::

## Overview

The Fig Client Testing framework enables you to:

- Test display scripts in isolation without running Fig.Web
- Validate script logic against different setting configurations
- Run automated tests in CI/CD pipelines
- Debug script behavior more easily
- Ensure script reliability before deployment

The framework supports two approaches:

1. **Settings Class Approach (Recommended)**: Use your actual `SettingsBase` classes for type-safe testing with full attribute support
2. **Manual Configuration**: Manually configure individual settings when needed

## Getting Started

### Prerequisites

Before you can test your display scripts, you need to add the Fig Client Testing package to your test project:

```xml
<PackageReference Include="Fig.Client.Testing" Version="latest" />
<PackageReference Include="NUnit" Version="3.13.3" />
<PackageReference Include="NUnit3TestAdapter" Version="4.2.1" />
```

:::tip Package Usage
The `Fig.Client.Testing` package provides testing utilities for both display script testing (covered in this guide) and [integration testing](./4-integration-testing.md) of ASP.NET Core applications. You only need to install this package once per test project to support both testing scenarios.
:::

## Recommended Approach: Using Settings Classes

The preferred way to test display scripts is by using your actual `SettingsBase` classes. This approach provides:

- **Type Safety**: Compile-time checking of property names and types
- **Attribute Preservation**: Automatic handling of validation, categories, secrets, etc.
- **Maintainability**: Changes to settings automatically reflect in tests
- **Real-world Accuracy**: Tests mirror actual client configuration

### Basic Settings Class Test

```csharp
using Fig.Client;
using Fig.Client.Attributes;
using Fig.Client.Testing;
using NUnit.Framework;

// Define your settings class with display scripts as constants
public class ApiSettings : SettingsBase
{
    // Display Script Constants
    private const string HttpsValidationScript = @"
        if (RequireHttps.Value && !BaseUrl.Value.toString().startsWith('https://')) {
            BaseUrl.ValidationExplanation = 'HTTPS is required when RequireHttps is enabled';
            BaseUrl.IsValid = false;
        } else {
            BaseUrl.ValidationExplanation = '';
            BaseUrl.IsValid = true;
        }";

    private const string DebugVisibilityScript = @"
        if (DebugLogging.Value) {
            DatabaseConnection.IsVisible = true;
            DatabaseConnection.CategoryName = 'Debug';
            DatabaseConnection.CategoryColor = '#FF9800';
        } else {
            DatabaseConnection.IsVisible = false;
        }";

    public override string ClientDescription => "API Configuration";

    [Setting("The base URL for the API")]
    [DisplayScript(HttpsValidationScript)]
    public string BaseUrl { get; set; } = "https://api.example.com";

    [Setting("Enable HTTPS requirement")]
    public bool RequireHttps { get; set; } = true;

    [Setting("API timeout in seconds")]
    [Validation(@"^\d+$", "Must be a positive number")]
    public int TimeoutSeconds { get; set; } = 30;

    [Setting("Database connection string")]
    [Secret]
    [DisplayScript(DebugVisibilityScript)]
    public string DatabaseConnection { get; set; } = "Server=localhost;";

    [Setting("Enable debug logging")]
    [Advanced]
    public bool DebugLogging { get; set; } = false;

    // Public methods to expose scripts for testing
    public static string GetHttpsValidationScript() => HttpsValidationScript;
    public static string GetDebugVisibilityScript() => DebugVisibilityScript;

    public override IEnumerable<string> GetValidationErrors() => new List<string>();
}

[TestFixture]
public class ApiSettingsTests
{
    [Test]
    public void Should_Validate_HTTPS_Requirement()
    {
        // Arrange
        var settings = new ApiSettings();
        var testRunner = new ClientTestRunner();

        // Act
        var testClient = testRunner.CreateClient(settings)
            .WithSetting("RequireHttps", true)
            .WithSetting("BaseUrl", "http://insecure.example.com")
            .Build();

        // Use the script defined in the settings class
        testRunner.ExecuteScript(testClient, ApiSettings.GetHttpsValidationScript());

        // Assert
        var baseUrlSetting = testClient.GetSetting("BaseUrl");
        Assert.IsFalse(baseUrlSetting.IsValid);
        Assert.That(baseUrlSetting.ValidationExplanation, Contains.Substring("HTTPS is required"));
    }

    [Test]
    public void Should_Show_Debug_Settings_When_Enabled()
    {
        // Arrange
        var settings = new ApiSettings();
        var testRunner = new ClientTestRunner();

        // Act
        var testClient = testRunner.CreateClient(settings)
            .WithSetting("DebugLogging", true)
            .Build();

        // Use the debug visibility script from the settings class
        testRunner.ExecuteScript(testClient, ApiSettings.GetDebugVisibilityScript());

        // Assert
        var dbSetting = testClient.GetSetting("DatabaseConnection");
        Assert.IsTrue(dbSetting.IsVisible);
        Assert.That(dbSetting.CategoryName, Is.EqualTo("Debug"));
    }
}
```

### Advanced Settings Class Examples

#### Complex Validation Logic

```csharp
public class ApiSettings : SettingsBase
{
    private const string ComplexValidationScript = @"
        // Multi-field validation
        if (RequireHttps.Value && !BaseUrl.Value.toString().startsWith('https://')) {
            BaseUrl.ValidationExplanation = 'HTTPS required when RequireHttps is enabled';
            BaseUrl.IsValid = false;
        }
        
        // Range validation
        if (TimeoutSeconds.Value < 10) {
            TimeoutSeconds.ValidationExplanation = 'Minimum timeout is 10 seconds';
            TimeoutSeconds.IsValid = false;
        }";

    [Setting("The base URL for the API")]
    [DisplayScript(ComplexValidationScript)]
    public string BaseUrl { get; set; } = "https://api.example.com";

    [Setting("API timeout in seconds")]
    [DisplayScript(ComplexValidationScript)]
    public int TimeoutSeconds { get; set; } = 30;

    // ... other properties ...

    public static string GetComplexValidationScript() => ComplexValidationScript;
}

[Test]
public void Should_Apply_Complex_Validation_Rules()
{
    var settings = new ApiSettings();
    var testRunner = new ClientTestRunner();

    var testClient = testRunner.CreateClient(settings)
        .WithSetting("RequireHttps", true)
        .WithSetting("BaseUrl", "http://insecure.com")
        .WithSetting("TimeoutSeconds", 5)
        .Build();

    // Use the validation script defined in the settings class
    testRunner.ExecuteScript(testClient, ApiSettings.GetComplexValidationScript());

    Assert.IsFalse(testClient.GetSetting("BaseUrl").IsValid);
    Assert.IsFalse(testClient.GetSetting("TimeoutSeconds").IsValid);
}
```

#### Conditional Visibility and Categories

```csharp
public class ApiSettings : SettingsBase
{
    private const string ProductionModeScript = @"
        var isProduction = !DebugLogging.Value;
        
        if (isProduction) {
            // Hide sensitive settings in production
            DatabaseConnection.IsVisible = false;
            DatabaseConnection.Advanced = true;
            
            // Organize by priority
            RequireHttps.DisplayOrder = 1;
            BaseUrl.DisplayOrder = 2;
            TimeoutSeconds.DisplayOrder = 3;
            
            // Production category styling
            RequireHttps.CategoryName = 'Production';
            RequireHttps.CategoryColor = '#E91E63';
        }";

    [Setting("Enable debug logging")]
    [Advanced]
    [DisplayScript(ProductionModeScript)]
    public bool DebugLogging { get; set; } = false;

    // ... other properties ...

    public static string GetProductionModeScript() => ProductionModeScript;
}

[Test]
public void Should_Control_Setting_Visibility_And_Organization()
{
    var settings = new ApiSettings();
    var testRunner = new ClientTestRunner();

    var testClient = testRunner.CreateClient(settings)
        .WithSetting("DebugLogging", false)
        .Build();

    // Use the production mode script from the settings class
    testRunner.ExecuteScript(testClient, ApiSettings.GetProductionModeScript());

    var dbSetting = testClient.GetSetting("DatabaseConnection");
    var httpsSetting = testClient.GetSetting("RequireHttps");
    
    Assert.IsFalse(dbSetting.IsVisible);
    Assert.IsTrue(dbSetting.Advanced);
    Assert.That(httpsSetting.DisplayOrder, Is.EqualTo(1));
    Assert.That(httpsSetting.CategoryName, Is.EqualTo("Production"));
}
```

#### TimeSpan and Type Handling

```csharp
public class ApiSettings : SettingsBase
{
    private const string TimeoutConversionScript = @"
        // TimeSpan values are provided as milliseconds in scripts
        var timeoutInSeconds = RequestTimeout.Value / 1000;
        if (timeoutInSeconds > 60) {
            TimeoutSeconds.Value = timeoutInSeconds;
        }";

    [Setting("Request timeout")]
    [DisplayScript(TimeoutConversionScript)]
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    [Setting("API timeout in seconds")]
    public int TimeoutSeconds { get; set; } = 30;

    // ... other properties ...

    public static string GetTimeoutConversionScript() => TimeoutConversionScript;
}

[Test]
public void Should_Handle_TimeSpan_Settings()
{
    var settings = new ApiSettings();
    var testRunner = new ClientTestRunner();

    var testClient = testRunner.CreateClient(settings)
        .WithSetting("RequestTimeout", TimeSpan.FromMinutes(2))
        .Build();

    // Use the timeout conversion script from the settings class
    testRunner.ExecuteScript(testClient, ApiSettings.GetTimeoutConversionScript());

    var timeoutValue = testClient.GetSetting("TimeoutSeconds").GetValue();
    Assert.That(timeoutValue, Is.EqualTo(120)); // 2 minutes = 120 seconds
}
```

## Alternative Approach: Manual Configuration

When you need more control or don't have a settings class available, you can manually configure settings:

### Basic Manual Test Structure

```csharp
[TestFixture]
public class ManualDisplayScriptTests
{
    [Test]
    public void Should_Hide_Setting_When_Condition_Is_False()
    {
        // Arrange
        var testRunner = new DisplayScriptTestRunner();
        var client = testRunner.CreateTestClient("TestClient")
            .AddBoolSetting("EnableFeature", false)
            .AddStringSetting("FeatureConfig", "default");

        var script = @"
            if (!EnableFeature.Value) {
                FeatureConfig.IsVisible = false;
            }
        ";

        // Act
        testRunner.RunScript(script, client);

        // Assert
        Assert.IsFalse(client.GetSetting("FeatureConfig").IsVisible);
    }
}
```

## Manual Setting Types

The testing framework supports all Fig setting types when using manual configuration:

### String Settings

```csharp
client.AddStringSetting("MyString", "initial value");
```

### Boolean Settings

```csharp
client.AddBoolSetting("MyBool", true);
```

### Integer Settings

```csharp
client.AddIntSetting("MyInt", 42);
```

### Double Settings

```csharp
client.AddDoubleSetting("MyDouble", 3.14);
```

### DateTime Settings

```csharp
client.AddDateTimeSetting("MyDateTime", DateTime.Now);
```

### TimeSpan Settings

```csharp
client.AddTimeSpanSetting("MyTimeSpan", TimeSpan.FromMinutes(30));
```

### JSON Settings

```csharp
var jsonValue = new { name = "test", value = 123 };
client.AddJsonSetting("MyJson", jsonValue);
```

### Data Grid Settings

```csharp
var row1 = testRunner.CreateDataGridRow(new Dictionary<string, object?>
{
    ["Name"] = "John",
    ["Age"] = 30,
    ["Active"] = true
});

var row2 = testRunner.CreateDataGridRow(new Dictionary<string, object?>
{
    ["Name"] = "Jane", 
    ["Age"] = 25,
    ["Active"] = false
});

client.AddDataGridSetting("Users", new List<Dictionary<string, IDataGridValueModel>> { row1, row2 });
```

## Advanced Testing Scenarios

### Testing Multiple Settings Classes

```csharp
[Test]
public void Should_Support_Multiple_Settings_Classes()
{
    var apiSettings = new ApiSettings();
    var dbSettings = new DatabaseSettings();
    var testRunner = new ClientTestRunner();

    var apiClient = testRunner.CreateClient(apiSettings, "ApiClient")
        .WithSetting("BaseUrl", "https://api.test.com")
        .Build();
        
    var dbClient = testRunner.CreateClient(dbSettings, "DatabaseClient")
        .WithSetting("ConnectionString", "Server=test;Database=TestDb;")
        .Build();

    Assert.That(apiClient.Name, Is.EqualTo("ApiClient"));
    Assert.That(dbClient.Name, Is.EqualTo("DatabaseClient"));
}
```

### Testing Validation Scripts (Manual Approach)

```csharp
[Test]
public void Should_Show_Validation_Error_For_Invalid_Value()
{
    var testRunner = new DisplayScriptTestRunner();
    var client = testRunner.CreateTestClient("ValidationClient")
        .AddStringSetting("Email", "invalid-email");

    var validationScript = @"
        if (!Email.Value.toString().match(/^[^\s@]+@[^\s@]+\.[^\s@]+$/)) {
            Email.ValidationExplanation = 'Please enter a valid email address';
            Email.IsValid = false;
        }
    ";

    testRunner.RunScript(validationScript, client);

    var emailSetting = client.GetSetting("Email");
    Assert.IsNotNull(emailSetting.ValidationExplanation);
    Assert.AreEqual("Please enter a valid email address", emailSetting.ValidationExplanation);
    Assert.IsFalse(emailSetting.IsValid);
}
```

### Testing Conditional Visibility (Manual Approach)

```csharp
[Test]
public void Should_Show_Advanced_Settings_When_Mode_Is_Advanced()
{
    var testRunner = new DisplayScriptTestRunner();
    var client = testRunner.CreateTestClient("ConditionalClient")
        .AddStringSetting("Mode", "Advanced")
        .AddStringSetting("AdvancedOption1", "value1")
        .AddStringSetting("AdvancedOption2", "value2");

    var script = @"
        var isAdvanced = Mode.Value === 'Advanced';
        AdvancedOption1.IsVisible = isAdvanced;
        AdvancedOption2.IsVisible = isAdvanced;
    ";

    testRunner.RunScript(script, client);

    Assert.IsTrue(client.GetSetting("AdvancedOption1").IsVisible);
    Assert.IsTrue(client.GetSetting("AdvancedOption2").IsVisible);
}
```

### Testing Data Grid Filtering (Manual Approach)

```csharp
[Test]
public void Should_Filter_DataGrid_Based_On_Criteria()
{
    var testRunner = new DisplayScriptTestRunner();
    
    var activeRow = testRunner.CreateDataGridRow(new Dictionary<string, object?>
    {
        ["Name"] = "Active User",
        ["Status"] = "Active"
    });
    
    var inactiveRow = testRunner.CreateDataGridRow(new Dictionary<string, object?>
    {
        ["Name"] = "Inactive User", 
        ["Status"] = "Inactive"
    });

    var client = testRunner.CreateTestClient("DataGridClient")
        .AddBoolSetting("ShowOnlyActive", true)
        .AddDataGridSetting("Users", new List<Dictionary<string, IDataGridValueModel>> { activeRow, inactiveRow });

    var script = @"
        if (ShowOnlyActive.Value) {
            // Filter logic would be implemented here
            // This is a simplified example
        }
    ";

    testRunner.RunScript(script, client);

    var usersSetting = client.GetDataGridSetting("Users");
    // Assert based on your filtering implementation
}
```

## Running Tests

### Command Line

```bash
dotnet test YourTestProject.csproj
```

### Visual Studio

Use the Test Explorer to run tests individually or in groups.

### CI/CD Integration

The testing framework works seamlessly with any CI/CD system that supports .NET testing:

```yaml
# GitHub Actions example
- name: Run Display Script Tests
  run: dotnet test tests/YourDisplayScriptTests.csproj --logger trx --results-directory TestResults
```

## Best Practices

1. **Prefer Settings Classes**: Use your actual `SettingsBase` classes whenever possible for type safety and attribute preservation
2. **Define Scripts as Constants**: Store display scripts as private constants in your settings class and expose them through public static methods for testing
3. **Use DisplayScript Attributes**: Apply the `[DisplayScript]` attribute to properties that need display logic, referencing the script constants
4. **Test Edge Cases**: Include tests for boundary conditions and invalid inputs
5. **Use Descriptive Names**: Make test names clearly describe what is being tested
6. **Keep Tests Focused**: Each test should verify one specific behavior
7. **Test Both Positive and Negative Cases**: Verify that scripts work correctly and fail appropriately
8. **Use Setup and Teardown**: Group common setup logic in `[SetUp]` methods
9. **Access Setting Values**: Remember to use `.Value` when accessing or setting values in scripts (e.g., `MySetting.Value`)
10. **Leverage Attribute Validation**: Let Fig's built-in validation attributes handle basic validation, focus scripts on complex business logic

### Display Script Organization

Organize your display scripts as constants within your settings class:

```csharp
public class MySettings : SettingsBase
{
    // Group related scripts together
    private const string ValidationScript = @"/* validation logic */";
    private const string VisibilityScript = @"/* visibility logic */";
    private const string FormattingScript = @"/* formatting logic */";

    [Setting("My setting")]
    [DisplayScript(ValidationScript)]
    public string MySetting { get; set; } = "default";

    // Expose scripts for testing
    public static string GetValidationScript() => ValidationScript;
    public static string GetVisibilityScript() => VisibilityScript;
    public static string GetFormattingScript() => FormattingScript;
}
```

### Script Syntax Guidelines

When writing test scripts, remember:

```csharp
// ✅ Correct: Access setting values and reference scripts from settings class
var testClient = testRunner.CreateClient(settings).Build();
testRunner.ExecuteScript(testClient, MySettings.GetValidationScript());

// ❌ Incorrect: Inline scripts in tests
var script = @"if (MySetting.Value) { /* logic */ }";
testRunner.ExecuteScript(testClient, script);
```

## Troubleshooting

### Common Issues

**Script execution errors**: Check that your JavaScript syntax is correct and all referenced settings exist in your test client. Ensure you're using `.Value` to access setting values.

**Type conversion issues**: Ensure that setting values match the expected types in your scripts. TimeSpan values are provided as milliseconds in JavaScript.

**Missing settings**: Verify that all settings referenced in your script have been added to the test client or exist in your settings class.

**Attribute not preserved**: When using the settings class approach, attributes should be automatically preserved. If using manual configuration, you may need to manually set advanced, secret, or validation properties.

**TimeSpan handling**: TimeSpan values in scripts are represented as milliseconds. Convert appropriately: `var seconds = TimeSpanSetting.Value / 1000;`

## Examples

For complete working examples, see the `Fig.Client.Testing.Example` project in the Fig repository, which includes comprehensive test cases covering:

### Settings Class Examples (Recommended)

- `EnhancedApiTests.cs`: Complete examples using `SettingsBase` classes
- Complex validation scenarios with multiple settings
- TimeSpan handling and type conversions
- Conditional visibility and categorization
- Multiple settings classes in a single test suite

### Manual Configuration Examples

- `DisplayScriptExampleTests.cs`: Traditional manual configuration approach
- Basic setting manipulation and validation
- Data grid filtering and manipulation
- Individual setting type examples

Both approaches demonstrate the full range of Fig's display script testing capabilities, with the settings class approach being the recommended method for new projects.

## Related Testing Approaches

While this guide focuses on testing your Fig display scripts, you may also want to test other aspects of your Fig-enabled applications:

### Integration Testing

If you're building ASP.NET Core applications that use Fig for configuration, you should also consider integration testing your application with Fig's configuration providers. The same `Fig.Client.Testing` package that you've installed for display script testing also provides tools for integration testing.

See the [Integration Testing](./4-integration-testing.md) guide to learn how to:

- Test ASP.NET Core applications with Fig configuration
- Use Fig's reloadable configuration provider in tests
- Disable Fig and inject test configuration
- Write comprehensive integration tests for Fig-enabled apps

Both testing approaches complement each other: display script testing ensures your Fig UI logic works correctly, while integration testing ensures your application behaves correctly with Fig configuration.

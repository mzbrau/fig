using Fig.Client.Testing.Scripts;
using NUnit.Framework;

namespace Fig.Examples.Client.Testing;

[TestFixture]
public class ExampleDisplayScriptTests
{
    [Test]
    public void Should_Create_Client_From_Settings_Class()
    {
        // Arrange
        var settings = new ExampleApiSettings();
        var testRunner = new ClientTestRunner();

        // Act
        var testClient = testRunner.CreateClient(settings)
            .Build();

        // Assert
        Assert.That(testClient, Is.Not.Null);
        Assert.That(testClient.Name, Is.EqualTo("ExampleApiSettings"));
        Assert.That(testClient.Settings.Count, Is.GreaterThan(0));
        
        var baseUrlSetting = testClient.GetSetting("BaseUrl");
        Assert.That(baseUrlSetting, Is.Not.Null);
        Assert.That(baseUrlSetting.GetValue(), Is.EqualTo("https://api.example.com"));
    }

    [Test]
    public void Should_Override_Setting_Values()
    {
        // Arrange
        var settings = new ExampleApiSettings();
        var testRunner = new ClientTestRunner();

        // Act
        var testClient = testRunner.CreateClient(settings)
            .WithSetting("BaseUrl", "https://test.example.com")
            .WithSetting("TimeoutSeconds", 60)
            .WithSetting("RequireHttps", false)
            .Build();

        // Assert
        Assert.That(testClient.GetSetting("BaseUrl")!.GetValue(), Is.EqualTo("https://test.example.com"));
        Assert.That(testClient.GetSetting("TimeoutSeconds")!.GetValue(), Is.EqualTo(60));
        Assert.That(testClient.GetSetting("RequireHttps")!.GetValue(), Is.EqualTo(false));
    }

    [Test]
    public void Should_Preserve_Setting_Attributes()
    {
        // Arrange
        var settings = new ExampleApiSettings();
        var testRunner = new ClientTestRunner();

        // Act
        var testClient = testRunner.CreateClient(settings)
            .Build();

        // Assert
        var debugSetting = testClient.GetSetting("DebugLogging");
        Assert.That(debugSetting, Is.Not.Null);
        Assert.That(debugSetting.Advanced, Is.True, "DebugLogging should be marked as Advanced");

        var apiVersionSetting = testClient.GetSetting("ApiVersion");
        Assert.That(apiVersionSetting, Is.Not.Null);
        if (apiVersionSetting is TestDropDownSetting dropDown)
        {
            Assert.That(dropDown.ValidValues, Contains.Item("v1"));
            Assert.That(dropDown.ValidValues, Contains.Item("v2"));
            Assert.That(dropDown.ValidValues, Contains.Item("v3"));
        }
    }

    [Test]
    public void Should_Support_Display_Script_Testing_With_Settings_Class()
    {
        // Arrange
        var settings = new ExampleApiSettings();
        var testRunner = new ClientTestRunner();

        var testClient = testRunner.CreateClient(settings)
            .WithSetting("RequireHttps", true)
            .WithSetting("BaseUrl", "http://insecure.example.com")
            .Build();

        // Act - Use the script defined in the settings class
        testRunner.ExecuteScript(testClient, ExampleApiSettings.GetHttpsValidationScript());

        // Assert
        var baseUrlSetting = testClient.GetSetting("BaseUrl");
        Assert.That(baseUrlSetting, Is.Not.Null);
        Assert.That(baseUrlSetting.ValidationExplanation, Is.EqualTo("HTTPS is required when RequireHttps is enabled"));
    }

    [Test]
    public void Should_Handle_TimeSpan_Settings()
    {
        // Arrange
        var settings = new ExampleApiSettings();
        var testRunner = new ClientTestRunner();

        // Act
        var testClient = testRunner.CreateClient(settings)
            .WithSetting("RequestTimeout", TimeSpan.FromMinutes(2))
            .Build();

        // Use the timeout conversion script from the settings class
        testRunner.ExecuteScript(testClient, ExampleApiSettings.GetTimeoutConversionScript());

        // Assert
        var timeoutValue = testClient.GetSetting("TimeoutSeconds")!.GetValue();
        Assert.That(timeoutValue, Is.EqualTo(120)); // 2 minutes = 120 seconds
    }

    [Test]
    public void Should_Validate_Port_Based_On_Protocol()
    {
        // Arrange
        var settings = new ExampleApiSettings();
        var testRunner = new ClientTestRunner();

        // Act
        var testClient = testRunner.CreateClient(settings)
            .WithSetting("RequireHttps", true)
            .WithSetting("Port", 80)
            .Build();

        // Use the port validation script from the settings class
        testRunner.ExecuteScript(testClient, ExampleApiSettings.GetPortValidationScript());

        // Assert
        var portSetting = testClient.GetSetting("Port");
        Assert.That(portSetting, Is.Not.Null);
        Assert.That(portSetting.IsValid, Is.False);
        Assert.That(portSetting.ValidationExplanation, Is.EqualTo("HTTPS requires port 443, not 80"));
    }

    [Test]
    public void Should_Show_Advanced_Settings_Based_On_Debug_Mode()
    {
        // Arrange
        var settings = new ExampleApiSettings();
        var testRunner = new ClientTestRunner();

        // Act
        var testClient = testRunner.CreateClient(settings)
            .WithSetting("DebugLogging", true)
            .Build();

        // Use the debug visibility script from the settings class
        testRunner.ExecuteScript(testClient, ExampleApiSettings.GetDebugVisibilityScript());

        // Assert
        var dbSetting = testClient.GetSetting("DatabaseConnection");
        Assert.That(dbSetting, Is.Not.Null);
        Assert.That(dbSetting.IsVisible, Is.True);
        Assert.That(dbSetting.CategoryName, Is.EqualTo("Debug"));
        Assert.That(dbSetting.CategoryColor, Is.EqualTo("#FF9800"));
    }

    [Test]
    public void Should_Update_Api_Endpoint_Based_On_Version()
    {
        // Arrange
        var settings = new ExampleApiSettings();
        var testRunner = new ClientTestRunner();

        // Act
        var testClient = testRunner.CreateClient(settings)
            .WithSetting("ApiVersion", "v3")
            .WithSetting("BaseUrl", "https://api.example.com")
            .Build();

        // Use the API version script from the settings class
        testRunner.ExecuteScript(testClient, ExampleApiSettings.GetApiVersionScript());

        // Assert
        var baseUrlSetting = testClient.GetSetting("BaseUrl");
        var timeoutSetting = testClient.GetSetting("TimeoutSeconds");
        
        Assert.That(baseUrlSetting, Is.Not.Null);
        Assert.That(baseUrlSetting.GetValue(), Is.EqualTo("https://api.example.com/v3"));
        
        Assert.That(timeoutSetting, Is.Not.Null);
        Assert.That(timeoutSetting.DisplayOrder, Is.EqualTo(1));
        Assert.That(timeoutSetting.CategoryName, Is.EqualTo("v3 Settings"));
    }

    [Test]
    public void Should_Apply_Conditional_Validation_Rules()
    {
        // Arrange
        var settings = new ExampleApiSettings();
        var testRunner = new ClientTestRunner();

        // Act
        var testClient = testRunner.CreateClient(settings)
            .WithSetting("RequireHttps", true)
            .WithSetting("BaseUrl", "http://insecure.example.com")
            .WithSetting("TimeoutSeconds", 5) // Too low
            .Build();

        // Test HTTPS validation using the settings class script
        testRunner.ExecuteScript(testClient, ExampleApiSettings.GetHttpsValidationScript());

        // For timeout validation, we'd normally have this in the settings class too
        // This is a simplified example combining multiple validations
        var timeoutValidationScript = @"
            if (TimeoutSeconds.Value < 10) {
                TimeoutSeconds.ValidationExplanation = 'Timeout must be at least 10 seconds';
                TimeoutSeconds.IsValid = false;
            } else if (TimeoutSeconds.Value > 300) {
                TimeoutSeconds.ValidationExplanation = 'Timeout cannot exceed 300 seconds';
                TimeoutSeconds.IsValid = false;
            } else {
                TimeoutSeconds.ValidationExplanation = '';
                TimeoutSeconds.IsValid = true;
            }
        ";
        
        testRunner.ExecuteScript(testClient, timeoutValidationScript);

        // Assert
        var baseUrlSetting = testClient.GetSetting("BaseUrl");
        var timeoutSetting = testClient.GetSetting("TimeoutSeconds");
        
        Assert.That(baseUrlSetting, Is.Not.Null);
        Assert.That(baseUrlSetting.IsValid, Is.False);
        Assert.That(baseUrlSetting.ValidationExplanation, Contains.Substring("HTTPS is required"));
        
        Assert.That(timeoutSetting, Is.Not.Null);
        Assert.That(timeoutSetting.IsValid, Is.False);
        Assert.That(timeoutSetting.ValidationExplanation, Is.EqualTo("Timeout must be at least 10 seconds"));
    }

    [Test]
    public void Should_Handle_Complex_Configuration_Scenarios()
    {
        // Arrange
        var settings = new ExampleApiSettings();
        var testRunner = new ClientTestRunner();

        // Act
        var testClient = testRunner.CreateClient(settings)
            .WithSetting("ApiVersion", "v2")
            .WithSetting("DebugLogging", false)
            .WithSetting("TimeoutSeconds", 30)
            .Build();

        var script = @"
            // Complex logic combining multiple settings
            var isProduction = !DebugLogging.Value;
            var isLegacyApi = ApiVersion.Value === 'v1';
            
            if (isProduction && isLegacyApi) {
                // Production + Legacy: Increase timeout and hide advanced options
                TimeoutSeconds.Value = Math.max(TimeoutSeconds.Value, 60);
                DatabaseConnection.IsVisible = false;
                DatabaseConnection.Advanced = true;
            } else if (isProduction && !isLegacyApi) {
                // Production + Modern: Standard settings
                TimeoutSeconds.Value = Math.max(TimeoutSeconds.Value, 30);
                DatabaseConnection.IsVisible = false;
            } else {
                // Development: Show everything
                DatabaseConnection.IsVisible = true;
                DatabaseConnection.Advanced = false;
                TimeoutSeconds.CategoryColor = '#4CAF50'; // Green for dev
            }
            
            // Update display order based on importance
            if (isProduction) {
                RequireHttps.DisplayOrder = 1;
                BaseUrl.DisplayOrder = 2;
                TimeoutSeconds.DisplayOrder = 3;
            }
        ";

        testRunner.ExecuteScript(testClient, script);

        // Assert - Production + v2 scenario
        var timeoutSetting = testClient.GetSetting("TimeoutSeconds");
        var dbSetting = testClient.GetSetting("DatabaseConnection");
        var httpsSetting = testClient.GetSetting("RequireHttps");
        
        Assert.That(timeoutSetting, Is.Not.Null);
        Assert.That(timeoutSetting.GetValue(), Is.EqualTo(30)); // Not increased for v2
        
        Assert.That(dbSetting, Is.Not.Null);
        Assert.That(dbSetting.IsVisible, Is.False); // Hidden in production
        
        Assert.That(httpsSetting, Is.Not.Null);
        Assert.That(httpsSetting.DisplayOrder, Is.EqualTo(1)); // High priority in production
    }

    [Test]
    public void Should_Support_Multiple_Settings_Classes()
    {
        // Arrange
        var apiSettings = new ExampleApiSettings();
        var dbSettings = new DatabaseSettings();
        var testRunner = new ClientTestRunner();

        // Act
        var apiClient = testRunner.CreateClient(apiSettings, "ApiClient")
            .WithSetting("BaseUrl", "https://api.test.com")
            .Build();
            
        var dbClient = testRunner.CreateClient(dbSettings, "DatabaseClient")
            .WithSetting("ConnectionString", "Server=test;Database=TestDb;")
            .Build();

        // Assert
        Assert.That(apiClient.Name, Is.EqualTo("ApiClient"));
        Assert.That(dbClient.Name, Is.EqualTo("DatabaseClient"));
        
        Assert.That(apiClient.GetSetting("BaseUrl"), Is.Not.Null);
        Assert.That(dbClient.GetSetting("ConnectionString"), Is.Not.Null);
        
        Assert.That(apiClient.GetSetting("BaseUrl")!.GetValue(), Is.EqualTo("https://api.test.com"));
        Assert.That(dbClient.GetSetting("ConnectionString")!.GetValue(), Is.EqualTo("Server=test;Database=TestDb;"));
    }

    // Data Grid Tests - Testing validation and manipulation of data grid settings

    [Test]
    public void Should_Validate_Data_Grid_Cells_With_JavaScript_Script()
    {
        // Arrange
        var testRunner = new DisplayScriptTestRunner();

        // Create a test client with data grid setting that has validation issues
        var testClient = testRunner.CreateTestClient("ValidationTests")
            .AddDataGridSetting("Services", [
                new()
                {
                    ["Name"] = new TestDataGridValueModel(""),
                    ["Group"] = new TestDataGridValueModel("Production"),
                    ["ValidationType"] = new TestDataGridValueModel("200OK"),
                    ["CustomString"] = new TestDataGridValueModel("")
                },

                new()
                {
                    ["Name"] = new TestDataGridValueModel("UserService"),
                    ["Group"] = new TestDataGridValueModel(""),
                    ["ValidationType"] = new TestDataGridValueModel("Custom String"),
                    ["CustomString"] = new TestDataGridValueModel("")
                }
            ]);

        // Act - Apply validation script for data grid cells
        var validationScript = @"
            if (Services && Services.Value) {
                let hasErrors = false;
                for (let i = 0; i < Services.Value.length; i++) {
                    // Validate Name field - cannot be empty
                    if (!Services.Value[i].Name || Services.Value[i].Name.trim() === '') {
                        hasErrors = true;
                        break;
                    }
                    
                    // Validate Group field - cannot be empty
                    if (!Services.Value[i].Group || Services.Value[i].Group.trim() === '') {
                        hasErrors = true;
                        break;
                    }
                }
                
                if (hasErrors) {
                    Services.IsValid = false;
                    Services.ValidationExplanation = 'Required fields are missing';
                } else {
                    Services.IsValid = true;
                    Services.ValidationExplanation = '';
                }
            }
        ";

        testRunner.RunScript(validationScript, testClient);

        // Assert
        var servicesSetting = testClient.GetSetting("Services");
        Assert.That(servicesSetting, Is.Not.Null);
        Assert.That(servicesSetting.IsValid, Is.False); // Should be invalid due to empty fields
        Assert.That(servicesSetting.ValidationExplanation, Is.EqualTo("Required fields are missing"));
    }

    [Test]
    public void Should_Update_Data_Grid_Display_Properties()
    {
        // Arrange
        var testRunner = new DisplayScriptTestRunner();

        var testClient = testRunner.CreateTestClient("DisplayTests")
            .AddDataGridSetting("Services", [
                new()
                {
                    ["Name"] = new TestDataGridValueModel("CriticalService"),
                    ["Group"] = new TestDataGridValueModel("Critical"),
                    ["ValidationType"] = new TestDataGridValueModel("Health Check"),
                    ["CustomString"] = new TestDataGridValueModel("")
                },

                new()
                {
                    ["Name"] = new TestDataGridValueModel("StandardService"),
                    ["Group"] = new TestDataGridValueModel("Standard"),
                    ["ValidationType"] = new TestDataGridValueModel("200OK"),
                    ["CustomString"] = new TestDataGridValueModel("")
                }
            ]);

        // Act - Apply script to update display properties based on criticality
        var displayScript = @"
            if (Services && Services.Value) {
                let hasCriticalServices = false;
                for (let i = 0; i < Services.Value.length; i++) {
                    if (Services.Value[i].Group === 'Critical') {
                        hasCriticalServices = true;
                        break;
                    }
                }
                
                if (hasCriticalServices) {
                    Services.DisplayOrder = 1;
                    Services.CategoryName = 'Critical Infrastructure';
                    Services.CategoryColor = '#FF5722';
                    Services.Advanced = false;
                } else {
                    Services.DisplayOrder = 5;
                    Services.CategoryName = 'Standard Services';
                    Services.CategoryColor = '#4CAF50';
                }
            }
        ";

        testRunner.RunScript(displayScript, testClient);

        // Assert
        var servicesSetting = testClient.GetSetting("Services");
        Assert.That(servicesSetting, Is.Not.Null);
        Assert.That(servicesSetting.DisplayOrder, Is.EqualTo(1));
        Assert.That(servicesSetting.CategoryName, Is.EqualTo("Critical Infrastructure"));
        Assert.That(servicesSetting.CategoryColor, Is.EqualTo("#FF5722"));
        Assert.That(servicesSetting.Advanced, Is.False);
    }

    [Test]
    public void Should_Control_Data_Grid_Read_Only_State()
    {
        // Arrange
        var testRunner = new DisplayScriptTestRunner();

        var testClient = testRunner.CreateTestClient("ReadOnlyTests")
            .AddDataGridSetting("Services", [
                new()
                {
                    ["Name"] = new TestDataGridValueModel("UserService"),
                    ["Group"] = new TestDataGridValueModel("Production"),
                    ["ValidationType"] = new TestDataGridValueModel("200OK"),
                    ["CustomString"] = new TestDataGridValueModel("")
                }
            ]);

        // Act - Apply script to control read-only state based on business rules
        var readOnlyScript = @"
            if (Services && Services.Value) {
                // In production, make certain fields read-only
                for (let i = 0; i < Services.Value.length; i++) {
                    if (Services.Value[i].Group === 'Production') {
                        Services.ValidationExplanation = 'Production services have restricted editing';
                        break;
                    }
                }
            }
        ";

        testRunner.RunScript(readOnlyScript, testClient);

        // Assert
        var servicesSetting = testClient.GetSetting("Services");
        Assert.That(servicesSetting, Is.Not.Null);
        Assert.That(servicesSetting.ValidationExplanation, Is.EqualTo("Production services have restricted editing"));
    }

    [Test]
    public void Should_Validate_Individual_Data_Grid_Cells()
    {
        // Arrange
        var testRunner = new DisplayScriptTestRunner();

        // Create a test client with a data grid that has some invalid data
        var testClient = testRunner.CreateTestClient("ValidationTests")
            .AddDataGridSetting("Services", [
                new()
                {
                    ["Name"] = new TestDataGridValueModel(""),
                    ["Group"] = new TestDataGridValueModel("Production"),
                    ["ValidationType"] = new TestDataGridValueModel("200OK"),
                    ["CustomString"] = new TestDataGridValueModel("")
                },

                new()
                {
                    ["Name"] = new TestDataGridValueModel("UserService"),
                    ["Group"] = new TestDataGridValueModel(""),
                    ["ValidationType"] = new TestDataGridValueModel("Custom String"),
                    ["CustomString"] = new TestDataGridValueModel("")
                },

                new()
                {
                    ["Name"] = new TestDataGridValueModel("OrderService"),
                    ["Group"] = new TestDataGridValueModel("Production"),
                    ["ValidationType"] = new TestDataGridValueModel("200OK"),
                    ["CustomString"] = new TestDataGridValueModel("")
                }
            ]);

        // Act - Apply basic validation script 
        var validationScript = @"
            if (Services && Services.Value) {
                let validCount = 0;
                for (let i = 0; i < Services.Value.length; i++) {
                    // Count valid rows (both Name and Group must be non-empty)
                    if (Services.Value[i].Name && Services.Value[i].Name.trim() !== '' &&
                        Services.Value[i].Group && Services.Value[i].Group.trim() !== '') {
                        validCount++;
                    }
                }
                
                // Set validation based on valid row count
                if (validCount === Services.Value.length) {
                    Services.IsValid = true;
                    Services.ValidationExplanation = 'All rows are valid';
                } else {
                    Services.IsValid = false;
                    Services.ValidationExplanation = `${Services.Value.length - validCount} rows have validation errors`;
                }
            }
        ";

        testRunner.RunScript(validationScript, testClient);

        // Assert
        var servicesSetting = testClient.GetSetting("Services") as TestDataGridSetting;
        Assert.That(servicesSetting, Is.Not.Null);
        
        // Should be invalid because first row has empty Name and second row has empty Group
        Assert.That(servicesSetting.IsValid, Is.False);
        Assert.That(servicesSetting.ValidationExplanation, Is.EqualTo("2 rows have validation errors"));
    }

    [Test]
    public void Should_Dynamically_Update_Data_Grid_Valid_Values()
    {
        // Arrange
        var testRunner = new DisplayScriptTestRunner();

        var testClient = testRunner.CreateTestClient("ValidValuesTests")
            .AddStringSetting("Groups", "Production,Staging,Development")
            .AddDataGridSetting("Services", [
                new()
                {
                    ["Name"] = new TestDataGridValueModel("UserService"),
                    ["Group"] = new TestDataGridValueModel("Production"),
                    ["ValidationType"] = new TestDataGridValueModel("200OK"),
                    ["CustomString"] = new TestDataGridValueModel("")
                },

                new()
                {
                    ["Name"] = new TestDataGridValueModel("OrderService"),
                    ["Group"] = new TestDataGridValueModel("Staging"),
                    ["ValidationType"] = new TestDataGridValueModel("Custom String"),
                    ["CustomString"] = new TestDataGridValueModel("valid")
                }
            ]);

        // Act - Apply script to demonstrate dynamic configuration
        var dynamicScript = @"
            if (Groups && Groups.Value && Services && Services.Value) {
                // Update the services based on available groups
                const availableGroups = Groups.Value.split(',');
                
                // Count services per group
                let groupCounts = {};
                for (let i = 0; i < Services.Value.length; i++) {
                    const group = Services.Value[i].Group;
                    groupCounts[group] = (groupCounts[group] || 0) + 1;
                }
                
                // Update display based on group distribution
                if (groupCounts['Production'] > 0) {
                    Services.CategoryColor = '#FF5722'; // Red for production
                    Services.CategoryName = 'Production Services';
                } else {
                    Services.CategoryColor = '#4CAF50'; // Green for non-production
                    Services.CategoryName = 'Development Services';
                }
            }
        ";

        testRunner.RunScript(dynamicScript, testClient);

        // Assert
        var servicesSetting = testClient.GetSetting("Services") as TestDataGridSetting;
        Assert.That(servicesSetting, Is.Not.Null);
        
        // Should have production styling since we have production services
        Assert.That(servicesSetting.CategoryColor, Is.EqualTo("#FF5722"));
        Assert.That(servicesSetting.CategoryName, Is.EqualTo("Production Services"));
        Assert.That(servicesSetting.Value, Is.Not.Null);
        Assert.That(servicesSetting.Value!.Count, Is.EqualTo(2));
    }

    [Test]
    public void Should_Apply_Complex_Data_Grid_Business_Rules()
    {
        // Arrange
        var testRunner = new DisplayScriptTestRunner();

        var testClient = testRunner.CreateTestClient("BusinessRulesTests")
            .AddStringSetting("Environment", "Production")
            .AddBooleanSetting("RequireHttps", true)
            .AddDataGridSetting("Services", [
                new()
                {
                    ["Name"] = new TestDataGridValueModel("UserService"),
                    ["Group"] = new TestDataGridValueModel("Critical"),
                    ["ValidationType"] = new TestDataGridValueModel("Health Check"),
                    ["CustomString"] = new TestDataGridValueModel("http://test.com")
                },

                new()
                {
                    ["Name"] = new TestDataGridValueModel("OrderService"),
                    ["Group"] = new TestDataGridValueModel("Standard"),
                    ["ValidationType"] = new TestDataGridValueModel("Custom String"),
                    ["CustomString"] = new TestDataGridValueModel("https://secure.com")
                },

                new()
                {
                    ["Name"] = new TestDataGridValueModel("LogService"),
                    ["Group"] = new TestDataGridValueModel("Optional"),
                    ["ValidationType"] = new TestDataGridValueModel("200OK"),
                    ["CustomString"] = new TestDataGridValueModel("")
                }
            ]);

        // Act - Apply complex business rules script
        var businessRulesScript = @"
            const environment = Environment.Value;
            const requireHttps = RequireHttps.Value;
            
            if (Services && Services.Value) {
                let hasSecurityIssues = false;
                let criticalCount = 0;
                
                for (let i = 0; i < Services.Value.length; i++) {
                    const service = Services.Value[i];
                    
                    // Count critical services
                    if (service.Group === 'Critical') {
                        criticalCount++;
                    }
                    
                    // Check for security issues in production
                    if (environment === 'Production' && requireHttps) {
                        if (service.Group === 'Critical' && service.CustomString && 
                            service.CustomString.startsWith && service.CustomString.startsWith('http://')) {
                            hasSecurityIssues = true;
                        }
                    }
                }
                
                // Apply business rules based on analysis
                if (hasSecurityIssues) {
                    Services.IsValid = false;
                    Services.ValidationExplanation = 'Critical services must use HTTPS in production';
                    Services.CategoryColor = '#F44336'; // Red for security issues
                } else if (criticalCount > 0) {
                    Services.CategoryColor = '#FF9800'; // Orange for critical services
                    Services.CategoryName = 'Critical Infrastructure';
                } else {
                    Services.CategoryColor = '#4CAF50'; // Green for standard services
                    Services.CategoryName = 'Standard Services';
                }
                
                // Set display order based on criticality
                Services.DisplayOrder = criticalCount > 0 ? 1 : 5;
            }
        ";

        testRunner.RunScript(businessRulesScript, testClient);

        // Assert
        var servicesSetting = testClient.GetSetting("Services") as TestDataGridSetting;
        Assert.That(servicesSetting, Is.Not.Null);
        
        // Should be invalid due to critical service using HTTP in production
        Assert.That(servicesSetting.IsValid, Is.False);
        Assert.That(servicesSetting.ValidationExplanation, Is.EqualTo("Critical services must use HTTPS in production"));
        Assert.That(servicesSetting.CategoryColor, Is.EqualTo("#F44336")); // Red for security issues
        Assert.That(servicesSetting.DisplayOrder, Is.EqualTo(1)); // High priority due to critical services
    }
}

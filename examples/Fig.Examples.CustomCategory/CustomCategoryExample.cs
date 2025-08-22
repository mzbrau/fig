using Fig.Client.Attributes;
using Fig.Client;
using System.Collections.Generic;

namespace Fig.Examples.CustomCategory
{
    /// <summary>
    /// Example demonstrating how to create and use custom categories with Fig.
    /// Custom categories allow you to define your own category enums with specific names and colors.
    /// </summary>
    public class CustomCategoryExampleSettings : SettingsBase
    {
        public override string ClientDescription => "Example showing custom category usage";

        // Example 1: Using custom category with helper method
        [Category("My Custom Database", "#FF6B35")]
        [Setting("Connection String")]
        public string DatabaseConnectionString { get; set; } = "Server=localhost;Database=MyApp;";

        // Example 2: Using the CategoryHelper to extract values from custom enum
        [Category("High Priority Feature", "#E74C3C")]
        [Setting("Feature Toggle")]
        public bool ImportantFeatureEnabled { get; set; } = true;

        // Example 3: Using standard Fig categories still works
        [Category(Fig.Client.Enums.Category.Security)]
        [Setting("API Key")]
        [Secret]
        public string ApiKey { get; set; } = "your-secret-key";

        // Example 4: Using CategoryColor enum for consistent colors
        [Category("Cache Settings", Fig.Client.Enums.CategoryColor.Purple)]
        [Setting("Cache TTL (minutes)")]
        public int CacheTtlMinutes { get; set; } = 30;

        public override IEnumerable<string> GetValidationErrors()
        {
            var errors = new List<string>();
            
            if (string.IsNullOrWhiteSpace(DatabaseConnectionString))
                errors.Add("Database connection string is required");
                
            if (CacheTtlMinutes <= 0)
                errors.Add("Cache TTL must be greater than 0");
                
            return errors;
        }
    }

    /// <summary>
    /// Example showing how to define your own category enum.
    /// This approach allows you to create reusable categories with consistent naming and colors.
    /// </summary>
    public enum MyCustomCategories
    {
        [CategoryName("Database Operations")]
        [ColorHex("#3498DB")]
        DatabaseOps,
        
        [CategoryName("External APIs")]
        [ColorHex("#E67E22")]
        ExternalApi,
        
        [CategoryName("Business Rules")]
        [ColorHex("#27AE60")]
        BusinessRules,
        
        [CategoryName("Performance")]
        [ColorHex("#F39C12")]
        Performance
    }

    /// <summary>
    /// Example settings class that demonstrates how to use the CategoryHelper
    /// to extract category information from custom enums.
    /// </summary>
    public class AdvancedCustomCategorySettings : SettingsBase
    {
        public override string ClientDescription => "Advanced custom category example";

        // You can use the CategoryHelper to extract name and color from your enum
        [Category("Database Operations", "#3498DB")] // Equivalent to using MyCustomCategories.DatabaseOps with CategoryHelper
        [Setting("Max Connection Pool Size")]
        public int MaxConnectionPoolSize { get; set; } = 100;

        [Category("External APIs", "#E67E22")] // Equivalent to using MyCustomCategories.ExternalApi with CategoryHelper  
        [Setting("API Timeout (seconds)")]
        public int ApiTimeoutSeconds { get; set; } = 30;

        [Category("Business Rules", "#27AE60")] // Equivalent to using MyCustomCategories.BusinessRules with CategoryHelper
        [Setting("Order Processing Enabled")]
        public bool OrderProcessingEnabled { get; set; } = true;

        [Category("Performance", "#F39C12")] // Equivalent to using MyCustomCategories.Performance with CategoryHelper
        [Setting("Enable Caching")]
        public bool EnableCaching { get; set; } = true;

        public override IEnumerable<string> GetValidationErrors()
        {
            var errors = new List<string>();
            
            if (MaxConnectionPoolSize <= 0)
                errors.Add("Max connection pool size must be greater than 0");
                
            if (ApiTimeoutSeconds <= 0)
                errors.Add("API timeout must be greater than 0");
                
            return errors;
        }
    }
}

/// <summary>
/// Static class demonstrating how you can create helper methods for your custom categories.
/// This approach provides a clean API for your users.
/// </summary>
public static class MyCustomCategoryExtensions
{
    /// <summary>
    /// Creates a CategoryAttribute from your custom enum value.
    /// </summary>
    public static Fig.Client.Attributes.CategoryAttribute ToCategory(this Fig.Examples.CustomCategory.MyCustomCategories category)
    {
        return CategoryHelper.FromEnum(category);
    }
    
    /// <summary>
    /// Extension method to get just the name from your custom category.
    /// </summary>
    public static string GetDisplayName(this Fig.Examples.CustomCategory.MyCustomCategories category)
    {
        return CategoryHelper.GetName(category) ?? category.ToString();
    }
    
    /// <summary>
    /// Extension method to get just the color from your custom category.
    /// </summary>
    public static string GetColor(this Fig.Examples.CustomCategory.MyCustomCategories category)
    {
        return CategoryHelper.GetColorHex(category) ?? "#000000";
    }
}
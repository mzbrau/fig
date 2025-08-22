using Fig.Client.Attributes;
using Fig.Client.Enums;
using System;

// This is a simple demonstration of the new custom category functionality

// 1. Define your own custom category enum
public enum MyCustomCategories
{
    [CategoryName("Application Settings")]
    [ColorHex("#3498DB")]
    AppSettings,
    
    [CategoryName("Third Party APIs")]
    [ColorHex("#E67E22")]
    ThirdPartyApis,
    
    [CategoryName("Business Logic")]
    [ColorHex("#27AE60")]
    BusinessLogic
}

// 2. Use the CategoryHelper to work with your custom categories
public class CategoryHelperDemo
{
    public static void ShowCategoryHelperFunctionality()
    {
        var customCategory = MyCustomCategories.AppSettings;
        
        // Extract name and color from custom enum
        var name = CategoryHelper.GetName(customCategory);
        var color = CategoryHelper.GetColorHex(customCategory);
        
        Console.WriteLine($"Category: {customCategory}");
        Console.WriteLine($"Display Name: {name}");
        Console.WriteLine($"Color: {color}");
        
        // Create a CategoryAttribute from your enum
        var categoryAttribute = CategoryHelper.FromEnum(customCategory);
        Console.WriteLine($"CategoryAttribute - Name: {categoryAttribute.Name}, Color: {categoryAttribute.ColorHex}");
    }
}

// 3. Use in your settings classes
public class MyAppSettings : Fig.Client.SettingsBase
{
    public override string ClientDescription => "Demo of custom categories";

    // Option 1: Use custom name and color directly
    [Category("Application Settings", "#3498DB")]
    [Setting("App Name")]
    public string AppName { get; set; } = "My Application";

    // Option 2: Use predefined Fig categories (still works!)
    [Category(Category.Database)]
    [Setting("Connection String")]
    public string ConnectionString { get; set; } = "Server=localhost;";

    // Option 3: Use Fig color enum with custom name
    [Category("Cache Configuration", CategoryColor.Purple)]
    [Setting("Cache TTL")]
    public int CacheTtl { get; set; } = 300;

    public override System.Collections.Generic.IEnumerable<string> GetValidationErrors() => [];
}

// Example showing how to run this demo
public class Program
{
    public static void Main()
    {
        Console.WriteLine("=== Fig Custom Categories Demo ===");
        Console.WriteLine();
        
        CategoryHelperDemo.ShowCategoryHelperFunctionality();
        
        Console.WriteLine();
        Console.WriteLine("=== Settings with Custom Categories ===");
        
        var settings = new MyAppSettings();
        var dataContract = settings.CreateDataContract("CustomCategoryDemo");
        
        foreach (var setting in dataContract.Settings)
        {
            Console.WriteLine($"Setting: {setting.Name}");
            Console.WriteLine($"  Category: {setting.CategoryName ?? "None"}");
            Console.WriteLine($"  Color: {setting.CategoryColor ?? "None"}");
            Console.WriteLine($"  Value: {setting.Value?.GetValue()}");
            Console.WriteLine();
        }
    }
}
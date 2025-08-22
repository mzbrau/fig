# Custom Categories Example

This example demonstrates how to create and use custom categories with Fig. Custom categories allow you to define your own category enums with specific names and colors, giving you more control over how your settings are organized and displayed in the Fig UI.

## Overview

Fig now supports custom categories in addition to the predefined categories. You can:

1. Use the existing `CategoryAttribute(string name, string colorHex)` constructor with custom values
2. Create your own enum types decorated with `CategoryNameAttribute` and `ColorHexAttribute`
3. Use the `CategoryHelper` utility class to extract category information from your custom enums

## Basic Usage

### Using Custom Names and Colors Directly

```csharp
[Category("My Custom Database", "#FF6B35")]
[Setting("Connection String")]
public string DatabaseConnectionString { get; set; } = "Server=localhost;Database=MyApp;";
```

### Creating Custom Category Enums

```csharp
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
```

### Using CategoryHelper

```csharp
// Extract category information from your custom enum
var category = MyCustomCategories.DatabaseOps;
var name = CategoryHelper.GetName(category);        // "Database Operations"
var color = CategoryHelper.GetColorHex(category);   // "#3498DB"

// Create a CategoryAttribute from your enum
var categoryAttribute = CategoryHelper.FromEnum(category);
```

### Using in Settings

```csharp
public class MySettings : SettingsBase
{
    // Option 1: Use the extracted values directly
    [Category("Database Operations", "#3498DB")]
    [Setting("Connection Pool Size")]
    public int ConnectionPoolSize { get; set; } = 100;
    
    // Option 2: Extract programmatically (useful for code generation)
    // [Category(CategoryHelper.GetName(MyCustomCategories.DatabaseOps), 
    //          CategoryHelper.GetColorHex(MyCustomCategories.DatabaseOps))]
    
    // Standard Fig categories still work
    [Category(Fig.Client.Enums.Category.Security)]
    [Setting("API Key")]
    public string ApiKey { get; set; }
}
```

## API Reference

### CategoryHelper Class

#### Static Methods

- `GetName(Enum enumValue)` - Extracts the category name from an enum value decorated with `CategoryNameAttribute`
- `GetColorHex(Enum enumValue)` - Extracts the color hex value from an enum value decorated with `ColorHexAttribute`
- `FromEnum(Enum enumValue)` - Creates a `CategoryAttribute` instance from an enum value

### Public Attributes

- `CategoryNameAttribute` - Specifies the display name for a category enum value
- `ColorHexAttribute` - Specifies the hex color value for a category enum value

## Migration Guide

If you're currently using Fig categories, no changes are required. All existing functionality continues to work:

```csharp
// This still works exactly as before
[Category(Category.Database)]
[Setting("Connection String")]
public string ConnectionString { get; set; }

// This also still works
[Category("Custom Category", CategoryColor.Blue)]
[Setting("Some Setting")]
public string SomeSetting { get; set; }
```

## Best Practices

1. **Consistent Colors**: Define your color palette once in your custom enum to maintain visual consistency
2. **Meaningful Names**: Use descriptive category names that help users understand the setting's purpose
3. **Reusable Enums**: Create shared category enums for use across multiple settings classes
4. **Documentation**: Document your custom categories so other developers understand their intended use

## Color Guidelines

- Use hex color codes in the format `#RRGGBB`
- Consider accessibility and ensure good contrast with the Fig UI
- Use colors consistently across related settings
- Test colors in both light and dark themes if your Fig deployment supports theme switching

## Example Project Structure

```
MyApp/
├── Settings/
│   ├── DatabaseSettings.cs
│   ├── ApiSettings.cs
│   └── Categories/
│       ├── AppCategories.cs          // Your custom enum
│       └── CategoryExtensions.cs     // Optional helper extensions
```

This organization keeps your category definitions centralized and reusable across your application.
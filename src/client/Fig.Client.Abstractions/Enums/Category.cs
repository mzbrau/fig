using Fig.Client.Abstractions.Attributes;

namespace Fig.Client.Abstractions.Enums;

/// <summary>
/// Predefined categories with associated names and colors for common application domains.
/// Each category provides a consistent name and distinctive color for visual grouping in the UI.
/// </summary>
public enum Category
{
    [CategoryName("Elasticsearch")]
    [ColorHex("#f2ef83")]
    Elasticsearch,
    
    [CategoryName("Database")]
    [ColorHex("#4f51c9")]
    Database,
    
    [CategoryName("File Handling")]
    [ColorHex("#357535")]
    FileHandling,
    
    [CategoryName("Map")]
    [ColorHex("#b85a35")]
    Map,
    
    [CategoryName("Message Bus")]
    [ColorHex("#8a2d69")]
    MessageBus,
    
    [CategoryName("API Integration")]
    [ColorHex("#cc4e58")]
    ApiIntegration,
    
    [CategoryName("Logging")]
    [ColorHex("#969998")]
    Logging,
    
    [CategoryName("Business Logic")]
    [ColorHex("#2d8a5a")]
    BusinessLogic,
    
    [CategoryName("Testing")]
    [ColorHex("#c94f51")]
    Testing,
    
    [CategoryName("Property Mapping")]
    [ColorHex("#5a2d8a")]
    PropertyMapping,
    
    [CategoryName("REST API")]
    [ColorHex("#8a5a2d")]
    RestApi,
    
    [CategoryName("Authentication")]
    [ColorHex("#2d5a8a")]
    Authentication,
    
    [CategoryName("Security")]
    [ColorHex("#8a2d2d")]
    Security
}
using Fig.Client.Abstractions.Attributes;

namespace Fig.Examples.AspNetApi;

public enum MyCustomCategories
{
    [CategoryName("Payment Processing")]
    [ColorHex("#FF5733")]
    PaymentProcessing,

    [CategoryName("User Management")]
    [ColorHex("#33FF57")]
    UserManagement,

    [CategoryName("Audit & Compliance")]
    [ColorHex("#3357FF")]
    AuditCompliance
}
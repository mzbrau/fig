namespace Fig.Api.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SkipTransactionAttribute : Attribute;

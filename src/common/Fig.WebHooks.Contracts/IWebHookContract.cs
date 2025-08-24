namespace Fig.WebHooks.Contracts;

/// <summary>
/// Base interface for all webhook data contracts
/// </summary>
public interface IWebHookContract
{
    /// <summary>
    /// Indicates whether this webhook is a test webhook or a real event
    /// </summary>
    bool IsTest { get; }
}
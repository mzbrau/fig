using Fig.Contracts.Assistant;

namespace Fig.Web.Services.Assistant;

public interface IAssistantChatClient
{
    IReadOnlyList<AssistantChatMessageDataContract> Messages { get; }

    event Action? Changed;

    Task<AssistantStatusDataContract> GetStatusAsync(CancellationToken cancellationToken = default);

    Task SendAsync(
        string message,
        AssistantStreamCallbacks callbacks,
        CancellationToken cancellationToken = default);

    void Clear();
}

public sealed class AssistantStreamCallbacks
{
    public Func<AssistantProgressUpdate, Task>? Progress { get; init; }

    public Func<string, Task>? Token { get; init; }

    public Func<AssistantProposedActionDataContract, Task>? Action { get; init; }

    public Func<Task>? Done { get; init; }

    public Func<string, Task>? Error { get; init; }
}

public sealed record AssistantProgressUpdate(string Id, string Label, string Status);

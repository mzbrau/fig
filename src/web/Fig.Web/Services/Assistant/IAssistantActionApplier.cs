using Fig.Contracts.Assistant;

namespace Fig.Web.Services.Assistant;

public interface IAssistantActionApplier
{
    Task ApplyAsync(
        IReadOnlyCollection<AssistantProposedActionDataContract> actions,
        CancellationToken cancellationToken = default);
}

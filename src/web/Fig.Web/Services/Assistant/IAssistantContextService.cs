using Fig.Contracts.Assistant;

namespace Fig.Web.Services.Assistant;

public interface IAssistantContextService
{
    void Publish(AssistantUiContextDataContract context);

    AssistantUiContextDataContract BuildContext();
}

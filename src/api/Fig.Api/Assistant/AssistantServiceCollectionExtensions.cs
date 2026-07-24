using Fig.Api.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Fig.Api.Assistant;

public static class AssistantServiceCollectionExtensions
{
    public static IServiceCollection AddFigAssistant(this IServiceCollection services)
    {
        services.AddScoped<AssistantHistoryCompactor>();
        services.AddScoped<ILlmClient, OpenAiCompatibleLlmClient>();
        services.AddScoped<IAssistantChatService, AssistantChatService>();
        services.AddScoped<IAuthenticatedService>(sp =>
            (IAuthenticatedService)sp.GetRequiredService<IAssistantChatService>());
        services.AddScoped<IAssistantToolRegistry, AssistantToolRegistry>();

        services.AddHttpClient("FigAssistantLlm", client =>
        {
            client.Timeout = TimeSpan.FromMinutes(3);
        });
        services.AddHttpClient("FigAssistantDocs", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}

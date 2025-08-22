using Fig.Api.Datalayer.Repositories;
using Fig.Api.ExtensionMethods;
using Fig.Api.Utils;
using Fig.Api.WebHooks;
using Fig.Contracts.Health;
using Fig.Contracts.WebHook;
using Fig.Datalayer.BusinessEntities;
using Fig.WebHooks.Contracts;

namespace Fig.Api.Services;

public class WebHookDisseminationService : IWebHookDisseminationService
{
    private readonly IWebHookRepository _webHookRepository;
    private readonly IWebHookQueue _webHookQueue;
    private readonly ILogger<WebHookDisseminationService> _logger;

    public WebHookDisseminationService(
        IWebHookRepository webHookRepository,
        IWebHookQueue webHookQueue,
        ILogger<WebHookDisseminationService> logger)
    {
        _webHookRepository = webHookRepository;
        _webHookQueue = webHookQueue;
        _logger = logger;
    }

    public async Task NewClientRegistration(SettingClientBusinessEntity client)
    {
        const WebHookType type = WebHookType.NewClientRegistration;
        await QueueWebHook(type, new NewClientRegistrationWebHookData(client), 
            () => GetMatchingWebHooks(type, client));
    }

    public async Task UpdatedClientRegistration(SettingClientBusinessEntity client)
    {
        const WebHookType type = WebHookType.UpdatedClientRegistration;
        await QueueWebHook(type, new UpdatedClientRegistrationWebHookData(client),
            () => GetMatchingWebHooks(type, client));
    }

    public async Task SettingValueChanged(List<ChangedSetting> changes, SettingClientBusinessEntity client,
        string? username, string changeMessage)
    {
        const WebHookType type = WebHookType.SettingValueChanged;
        await QueueWebHook(type, new SettingValueChangedWebHookData
        {
            Changes = changes,
            Client = client,
            Username = username,
            ChangeMessage = changeMessage
        }, () => GetMatchingWebHooks(type, client));
    }

    public async Task ClientConnected(ClientRunSessionBusinessEntity session, ClientStatusBusinessEntity client)
    {
        const WebHookType type = WebHookType.ClientStatusChanged;
        await QueueWebHook(type, new ClientConnectedWebHookData(session, client),
            () => GetMatchingWebHooks(type, client));

        await QueueMinRunSessionsWebHook(client, ConnectionEvent.Connected);
    }

    public async Task ClientDisconnected(ClientRunSessionBusinessEntity session, ClientStatusBusinessEntity client)
    {
        const WebHookType type = WebHookType.ClientStatusChanged;
        await QueueWebHook(type, new ClientDisconnectedWebHookData(session, client),
            () => GetMatchingWebHooks(type, client));
        
        await QueueMinRunSessionsWebHook(client, ConnectionEvent.Disconnected);
    }

    public async Task HealthStatusChanged(ClientRunSessionBusinessEntity session, ClientStatusBusinessEntity client,
        HealthDataContract healthDetails)
    {
        const WebHookType type = WebHookType.HealthStatusChanged;
        await QueueWebHook(type, new HealthStatusChangedWebHookData(session, client, healthDetails),
            () => GetMatchingWebHooks(type, client));
    }

    public async Task SecurityEvent(SecurityEventWebHookData securityEvent)
    {
        const WebHookType type = WebHookType.SecurityEvent;
        await QueueWebHook(type, securityEvent, GetAllSecurityWebHooks);
    }

    private async Task<List<WebHookBusinessEntity>> GetAllSecurityWebHooks()
    {
        var webHooks = await _webHookRepository.GetWebHooksByType(WebHookType.SecurityEvent);
        return webHooks.ToList();
    }

    private async Task QueueMinRunSessionsWebHook(ClientStatusBusinessEntity client, ConnectionEvent connectionEvent)
    {
        var matchingWebHooks = await GetMatchingMinRunSessionsWebHooks(client);
        if (!matchingWebHooks.Any())
        {
            _logger.LogDebug("No matching MinRunSessions webhooks found for client {ClientName}", client.Name);
            return;
        }

        foreach (var webHook in matchingWebHooks)
        {
            await ProcessWebHookForSessionChange(client, connectionEvent, webHook);
        }
    }

    private async Task<List<WebHookBusinessEntity>> GetMatchingMinRunSessionsWebHooks(ClientStatusBusinessEntity client)
    {
        var allWebHooks = await _webHookRepository.GetWebHooksByType(WebHookType.MinRunSessions);
        return allWebHooks.Where(webHook => webHook.IsMatch(client)).ToList();
    }

    private async Task ProcessWebHookForSessionChange(ClientStatusBusinessEntity client, ConnectionEvent connectionEvent, WebHookBusinessEntity webHook)
    {
        var sessionCounts = new SessionCounts(client.RunSessions.Count, webHook.MinSessions);
        var eventDetails = DetermineRunSessionEvent(connectionEvent, sessionCounts);

        if (eventDetails == null)
            return;

        await QueueMinRunSessionsWebHookItem(client, webHook, eventDetails, sessionCounts);
    }

    private RunSessionEventDetails? DetermineRunSessionEvent(ConnectionEvent connectionEvent, SessionCounts sessionCounts)
    {
        return connectionEvent switch
        {
            ConnectionEvent.Connected => CheckForMinimumRestored(sessionCounts),
            ConnectionEvent.Disconnected => CheckForBelowMinimum(sessionCounts),
            _ => null
        };
    }

    private RunSessionEventDetails? CheckForMinimumRestored(SessionCounts sessionCounts)
    {
        // Check if we just reached or exceeded the minimum
        var previousCount = sessionCounts.CurrentCount - 1;
        if (sessionCounts.CurrentCount >= sessionCounts.MinimumRequired && previousCount < sessionCounts.MinimumRequired)
        {
            return new RunSessionEventDetails(RunSessionsEvent.MinimumRestored, sessionCounts.CurrentCount);
        }
        return null;
    }

    private RunSessionEventDetails? CheckForBelowMinimum(SessionCounts sessionCounts)
    {
        // Current count is BEFORE disconnection, so after disconnection it will be currentCount - 1
        var afterDisconnectionCount = sessionCounts.CurrentCount - 1;
        
        // Check if we are about to drop below the minimum
        if (sessionCounts.CurrentCount >= sessionCounts.MinimumRequired && afterDisconnectionCount < sessionCounts.MinimumRequired)
        {
            return new RunSessionEventDetails(RunSessionsEvent.BelowMinimum, afterDisconnectionCount);
        }
        return null;
    }

    private Task QueueMinRunSessionsWebHookItem(ClientStatusBusinessEntity client, WebHookBusinessEntity webHook, 
        RunSessionEventDetails eventDetails, SessionCounts sessionCounts)
    {
        var queueItem = new WebHookQueueItem
        {
            WebHookType = WebHookType.MinRunSessions,
            WebHookData = new MinRunSessionsWebHookData(client, eventDetails.Event, eventDetails.ReportedSessionCount),
            MatchingWebHooks = [webHook]
        };

        _webHookQueue.QueueWebHook(queueItem);
        _logger.LogDebug("Queued MinRunSessions webhook for client {ClientName}, event: {Event}, reportedCount: {ReportedCount}, actualCount: {ActualCount}, minimum: {MinSessions}", 
            client.Name, eventDetails.Event, eventDetails.ReportedSessionCount, sessionCounts.CurrentCount, sessionCounts.MinimumRequired);

        return Task.CompletedTask;
    }

    private record SessionCounts(int CurrentCount, int MinimumRequired);
    private record RunSessionEventDetails(RunSessionsEvent Event, int ReportedSessionCount);
    
    private async Task QueueWebHook(WebHookType webHookType, object webHookData,
        Func<Task<List<WebHookBusinessEntity>>> getMatchingWebHooks)
    {
        try
        {
            var matchingWebHooks = await getMatchingWebHooks();
            
            if (!matchingWebHooks.Any())
            {
                _logger.LogDebug("No matching webhooks found for type {WebHookType}", webHookType);
                return;
            }

            var queueItem = new WebHookQueueItem
            {
                WebHookType = webHookType,
                WebHookData = webHookData,
                MatchingWebHooks = matchingWebHooks
            };

            _webHookQueue.QueueWebHook(queueItem);
            _logger.LogDebug("Queued webhook of type {WebHookType} with {Count} matching webhooks", 
                webHookType, matchingWebHooks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error queueing webhook of type {WebHookType}", webHookType);
        }
    }

    private async Task<List<WebHookBusinessEntity>> GetMatchingWebHooks(
        WebHookType webHookType,
        SettingClientBusinessEntity client)
    {
        var webHooks = await _webHookRepository.GetWebHooksByType(webHookType);
        return webHooks.Where(a => a.IsMatch(client)).ToList();
    }
    
    private async Task<List<WebHookBusinessEntity>> GetMatchingWebHooks(
        WebHookType webHookType,
        ClientStatusBusinessEntity client)
    {
        var webHooks = await _webHookRepository.GetWebHooksByType(webHookType);
        return webHooks.Where(a => a.IsMatch(client)).ToList();
    }
}
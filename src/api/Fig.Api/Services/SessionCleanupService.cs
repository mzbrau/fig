using Fig.Api.Datalayer.Repositories;
using Fig.Api.ExtensionMethods;
using Fig.Api.Utils;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Services;

/// <summary>
/// Service for cleaning up expired client run sessions.
/// </summary>
public class SessionCleanupService : ISessionCleanupService
{
    private readonly ILogger<SessionCleanupService> _logger;
    private readonly IClientStatusRepository _clientStatusRepository;
    private readonly IEventLogRepository _eventLogRepository;
    private readonly IEventLogFactory _eventLogFactory;
    private readonly IWebHookDisseminationService _webHookDisseminationService;

    public SessionCleanupService(
        ILogger<SessionCleanupService> logger,
        IClientStatusRepository clientStatusRepository,
        IEventLogRepository eventLogRepository,
        IEventLogFactory eventLogFactory,
        IWebHookDisseminationService webHookDisseminationService)
    {
        _logger = logger;
        _clientStatusRepository = clientStatusRepository;
        _eventLogRepository = eventLogRepository;
        _eventLogFactory = eventLogFactory;
        _webHookDisseminationService = webHookDisseminationService;
    }

    public async Task<int> RemoveExpiredSessionsAsync()
    {
        _logger.LogDebug("Starting expired session cleanup");
        
        var clients = await _clientStatusRepository.GetAllClients(new ServiceUser());
        var totalRemoved = 0;

        foreach (var client in clients)
        {
            var expiredSessions = client.RunSessions.Where(s => s.IsExpired()).ToList();
            
            // Track the last expired session in case we're removing all sessions
            ClientRunSessionBusinessEntity? lastExpiredSession = null;
            
            foreach (var session in expiredSessions)
            {
                _logger.LogInformation("Removing expired session {RunSessionId} for client {ClientName}", 
                    session.RunSessionId, client.Name.Sanitize());
                
                // Call webhook BEFORE removing the session so it can see the correct "before" state
                try
                {
                    await _webHookDisseminationService.ClientDisconnected(session, client);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calling webhook for disconnected client {ClientName}", client.Name.Sanitize());
                }
                
                lastExpiredSession = session;
                client.RunSessions.Remove(session);
                
                try
                {
                    await _eventLogRepository.Add(_eventLogFactory.ExpiredSession(session, client));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error logging expired session event for client {ClientName}", client.Name.Sanitize());
                }
                
                totalRemoved++;
            }

            if (expiredSessions.Any())
            {
                // If all sessions have been removed, set the last run session disconnected timestamp and machine name
                if (client.RunSessions.Count == 0 && lastExpiredSession != null)
                {
                    client.LastRunSessionDisconnected = DateTime.UtcNow;
                    client.LastRunSessionMachineName = lastExpiredSession.Hostname;
                    _logger.LogInformation("All sessions expired for client {ClientName}. Setting LastRunSessionDisconnected to {Time} and LastRunSessionMachineName to {Machine}", 
                        client.Name.Sanitize(), client.LastRunSessionDisconnected, client.LastRunSessionMachineName);
                }
                
                try
                {
                    await _clientStatusRepository.UpdateClientStatus(client);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating client status for {ClientName} after removing expired sessions", 
                        client.Name.Sanitize());
                }
            }
        }

        if (totalRemoved > 0)
        {
            _logger.LogInformation("Expired session cleanup completed. Removed {TotalRemoved} sessions", totalRemoved);
        }
        else
        {
            _logger.LogDebug("Expired session cleanup completed. No expired sessions found");
        }
        
        return totalRemoved;
    }
}

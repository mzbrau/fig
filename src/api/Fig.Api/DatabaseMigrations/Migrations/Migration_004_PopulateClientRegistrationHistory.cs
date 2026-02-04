using Fig.Api.Datalayer.Repositories;
using Fig.Api.Utils;
using Fig.Common.NetStandard.Json;
using Fig.Contracts.ClientRegistrationHistory;
using Fig.Datalayer.BusinessEntities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Fig.Api.DatabaseMigrations.Migrations;

/// <summary>
/// Populates initial client registration history for all existing clients.
/// This ensures that clients registered before the history feature was introduced
/// will appear in the registration history list.
/// </summary>
public class Migration_004_PopulateClientRegistrationHistory : IDatabaseMigration
{
    public int ExecutionNumber => 4;
    
    public string Description => "Populate initial client registration history for all existing clients";
    
    public string SqlServerScript => string.Empty; // No SQL needed, using code execution
    
    public string SqliteScript => string.Empty; // No SQL needed, using code execution

    public async Task? ExecuteCode(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Migration_004_PopulateClientRegistrationHistory>>();
        var settingClientRepository = serviceProvider.GetRequiredService<ISettingClientRepository>();
        var historyRepository = serviceProvider.GetRequiredService<IClientRegistrationHistoryRepository>();
        
        logger.LogInformation("Starting population of client registration history for existing clients");
        
        try
        {
            // Get all clients - use ServiceUser to bypass user filtering
            var allClients = await settingClientRepository.GetAllClients(new ServiceUser(), upgradeLock: false, validateCode: false);
            
            if (!allClients.Any())
            {
                logger.LogInformation("No existing clients found. Skipping migration");
                return;
            }

            var existingHistory = await historyRepository.GetAll();
            var existingClientNames = new HashSet<string>(
                existingHistory.Select(h => h.ClientName),
                StringComparer.OrdinalIgnoreCase);
            
            // Group by client name to get unique clients (ignoring instances)
            // We want one history entry per unique client name/version combination
            var uniqueClients = allClients
                .GroupBy(c => c.Name)
                .Select(g => g.OrderByDescending(c => c.LastRegistration).First())
                .ToList();
            
            logger.LogInformation("Found {TotalClients} total client registrations, {UniqueClients} unique clients", 
                allClients.Count, uniqueClients.Count);
            
            var historyEntriesCreated = 0;
            var historyEntriesSkipped = 0;
            
            foreach (var client in uniqueClients)
            {
                try
                {
                    if (existingClientNames.Contains(client.Name))
                    {
                        historyEntriesSkipped++;
                        logger.LogDebug("History already exists for client {ClientName}. Skipping.", client.Name);
                        continue;
                    }

                    var settings = client.Settings
                        .Select(s => new SettingDefaultValueDataContract(
                            s.Name,
                            GetDefaultValueAsString(s),
                            s.Advanced
                        ))
                        .ToList();
                    
                    var history = new ClientRegistrationHistoryBusinessEntity
                    {
                        // Use LastRegistration date if available, otherwise use current time
                        RegistrationDateUtc = client.LastRegistration ?? DateTime.UtcNow,
                        ClientName = client.Name,
                        ClientVersion = GetClientVersion(client),
                        SettingsJson = JsonConvert.SerializeObject(settings, JsonSettings.FigDefault)
                    };
                    
                    await historyRepository.Add(history);
                    historyEntriesCreated++;
                    
                    logger.LogDebug("Created history entry for client {ClientName}", client.Name);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to create history entry for client {ClientName}. Continuing with remaining clients", 
                        client.Name);
                }
            }
            
            logger.LogInformation("Client registration history population completed. Created {Count} history entries, skipped {Skipped} existing clients", 
                historyEntriesCreated, historyEntriesSkipped);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to populate client registration history");
            throw;
        }
    }
    
    private static string? GetDefaultValueAsString(SettingBusinessEntity setting)
    {
        if (setting.DefaultValue == null)
            return null;

        try
        {
            return JsonConvert.SerializeObject(setting.DefaultValue.GetValue(), JsonSettings.FigDefault);
        }
        catch
        {
            return setting.DefaultValue.ToString();
        }
    }
    
    private static string GetClientVersion(SettingClientBusinessEntity client)
    {
        // Client version is stored in RunSessions - get the most recent one if available
        // ApplicationVersion represents the client application's version at runtime
        var latestSession = client.RunSessions
            .OrderByDescending(s => s.LastSeen)
            .FirstOrDefault();
        
        return latestSession?.ApplicationVersion ?? string.Empty;
    }
}

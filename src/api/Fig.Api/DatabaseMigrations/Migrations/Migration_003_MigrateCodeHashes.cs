using Fig.Api.DatabaseMigrations;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Utils;
using Fig.Api.Validators;
using Fig.Datalayer.BusinessEntities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fig.Api.DatabaseMigrations.Migrations;

public class Migration_003_MigrateCodeHashes : IDatabaseMigration
{
    public int ExecutionNumber => 3;
    
    public string Description => "Migrate display script hashes from slow BCrypt algorithm to new HMAC-SHA256 algorithm. This is a significant performance improvement";
    
    public string SqlServerScript => string.Empty; // No SQL needed, using code execution
    
    public string SqliteScript => string.Empty; // No SQL needed, using code execution

    public async Task? ExecuteCode(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Migration_003_MigrateCodeHashes>>();
        var settingClientRepository = serviceProvider.GetRequiredService<ISettingClientRepository>();
        var newCodeHasher = serviceProvider.GetRequiredService<ICodeHasher>();
        var legacyCodeHasher = serviceProvider.GetRequiredService<ILegacyCodeHasher>();
        
        logger.LogInformation("Starting code hash migration from legacy BCrypt to HMAC-SHA256");
        
        try
        {
            // Get all clients without user filtering (passing null) and with upgrade lock
            // Don't validate hashes during retrieval to prevent clearing scripts before migration
            var allClients = await settingClientRepository.GetAllClients(new ServiceUser(), true, false);
            var updatedClients = new List<SettingClientBusinessEntity>();
            var totalHashesMigrated = 0;
            
            foreach (var client in allClients)
            {
                var clientUpdated = false;
                
                foreach (var setting in client.Settings)
                {
                    // Only process settings that have a display script hash
                    if (string.IsNullOrWhiteSpace(setting.DisplayScriptHash) || 
                        string.IsNullOrWhiteSpace(setting.DisplayScript))
                        continue;
                    
                    try
                    {
                        // Try to validate with legacy hasher first
                        if (legacyCodeHasher.IsValid(setting.DisplayScriptHash, setting.DisplayScript))
                        {
                            // Hash is legacy format - migrate to new format
                            var newHash = newCodeHasher.GetHash(setting.DisplayScript);
                            setting.DisplayScriptHash = newHash;
                            clientUpdated = true;
                            totalHashesMigrated++;
                            
                            logger.LogDebug("Migrated hash for setting {SettingName} in client {ClientName}", 
                                setting.Name, client.Name);
                        }
                        else if (newCodeHasher.IsValid(setting.DisplayScriptHash, setting.DisplayScript))
                        {
                            // Hash is already in new format - no action needed
                            logger.LogTrace("Hash already migrated for setting {SettingName} in client {ClientName}", 
                                setting.Name, client.Name);
                        }
                        else
                        {
                            // Hash is invalid with both hashers - remove it and the script for security
                            logger.LogWarning("Invalid hash detected for setting {SettingName} in client {ClientName}. " +
                                            "Removing display script and hash for security", setting.Name, client.Name);
                            setting.DisplayScript = null;
                            setting.DisplayScriptHash = null;
                            clientUpdated = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing hash for setting {SettingName} in client {ClientName}. " +
                                          "Removing display script and hash for security", setting.Name, client.Name);
                        setting.DisplayScript = null;
                        setting.DisplayScriptHash = null;
                        clientUpdated = true;
                    }
                }
                
                if (clientUpdated)
                {
                    updatedClients.Add(client);
                }
            }
            
            // Update all modified clients
            foreach (var client in updatedClients)
            {
                await settingClientRepository.UpdateClient(client);
                logger.LogDebug("Updated client {ClientName} with migrated hashes", client.Name);
            }
            
            logger.LogInformation("Code hash migration completed successfully. " +
                                "Migrated {TotalHashes} hashes across {ClientCount} clients", 
                                totalHashesMigrated, updatedClients.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to migrate code hashes");
            throw;
        }
    }
}
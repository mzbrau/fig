using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fig.Client.Configuration;
using Fig.Client.ConfigurationProvider;
using Fig.Client.CustomActions;
using Fig.Client.Logging;
using Fig.Contracts.CustomActions;
using Microsoft.Extensions.Hosting;

namespace Fig.Client.Workers
{
    public class CustomActionExecutionWorker : BackgroundService
    {
        private readonly IApiCommunicationHandler _apiCommunicationHandler;
        private readonly IServiceProvider _serviceProvider; // Not directly used in provided snippet, but good to have if actions need DI.
        private readonly SettingsBase _settings;
        private readonly FigOptions _options; // Keep FigOptions for direct access after .Value
        private readonly IFigLogger<CustomActionExecutionWorker> _logger;

        public CustomActionExecutionWorker(IApiCommunicationHandler apiCommunicationHandler,
                                           IServiceProvider serviceProvider,
                                           SettingsBase settings,
                                           IOptions<FigOptions> options, // Updated to IOptions<FigOptions>
                                           IFigLogger<CustomActionExecutionWorker> logger)
        {
            _apiCommunicationHandler = apiCommunicationHandler;
            _serviceProvider = serviceProvider;
            _settings = settings;
            _options = options.Value; // Get FigOptions from IOptions
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Wait a short period for initial registration to complete.
            // Ideally, this worker would start after successful registration.
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            
            _logger.LogInformation("Custom Action Execution Worker starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // The interface IApiCommunicationHandler now has PollForCustomActionRequests and SendCustomActionResults.
                    // PollForCustomActionRequests now only takes a CancellationToken.
                    var pendingActions = await _apiCommunicationHandler.PollForCustomActionRequests(stoppingToken);

                    if (pendingActions != null && pendingActions.Any())
                    {
                        // Service scope creation to resolve transient ICustomAction instances
                        using var scope = _serviceProvider.CreateScope();
                        foreach (var polledAction in pendingActions)
                        {
                            ICustomAction? actionToExecute = null;
                            try
                            {
                                // Resolve the action from the scope to ensure it's a new instance if registered as transient
                                actionToExecute = _settings.CustomActions.FirstOrDefault(a => a.Name == polledAction.ActionName);
                                
                                if (actionToExecute == null) // Fallback or if actions are singletons registered directly
                                {
                                     actionToExecute = scope.ServiceProvider.GetServices<ICustomAction>().FirstOrDefault(a => a.Name == polledAction.ActionName);
                                }


                                if (actionToExecute != null)
                                {
                                    _logger.LogInformation($"Executing custom action '{polledAction.ActionName}' with execution ID '{polledAction.ExecutionId}'.");
                                    var results = await actionToExecute.Execute(polledAction.Settings, stoppingToken);
                                    var resultContract = new CustomActionClientExecuteRequestDataContract
                                    {
                                        ExecutionId = polledAction.ExecutionId,
                                        Results = results.ToList(),
                                        ExecutedAt = DateTime.UtcNow,
                                        Success = true
                                    };
                                    await _apiCommunicationHandler.SendCustomActionResults(resultContract, stoppingToken);
                                    _logger.LogInformation($"Successfully executed and sent results for custom action '{polledAction.ActionName}', execution ID '{polledAction.ExecutionId}'.");
                                }
                                else
                                {
                                    _logger.LogError($"Custom action '{polledAction.ActionName}' requested by API was not found in registered client actions nor resolvable via DI.");
                                    var errorResult = new CustomActionClientExecuteRequestDataContract
                                    {
                                        ExecutionId = polledAction.ExecutionId,
                                        ExecutedAt = DateTime.UtcNow,
                                        Success = false,
                                        ErrorMessage = $"Action '{polledAction.ActionName}' not found on client."
                                    };
                                    await _apiCommunicationHandler.SendCustomActionResults(errorResult, stoppingToken);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Error executing custom action '{actionToExecute?.Name ?? polledAction.ActionName}' with execution ID '{polledAction.ExecutionId}'. Error: {ex.Message}");
                                var errorResult = new CustomActionClientExecuteRequestDataContract
                                {
                                    ExecutionId = polledAction.ExecutionId,
                                    ExecutedAt = DateTime.UtcNow,
                                    Success = false,
                                    ErrorMessage = ex.Message
                                };
                                // Ensure results are sent even if there's an exception during execution.
                                await _apiCommunicationHandler.SendCustomActionResults(errorResult, stoppingToken);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error in Custom Action Execution Worker polling loop: {ex.Message}");
                    // Avoid tight loop in case of persistent errors unrelated to specific action execution
                }
                
                await Task.Delay(_options.CustomActionPollInterval, stoppingToken);
            }
            _logger.LogInformation("Custom Action Execution Worker stopping.");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fig.Client.ConfigurationProvider;
using Fig.Client.CustomActions;
using Fig.Client.ExtensionMethods;
using Fig.Contracts.CustomActions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fig.Client.Workers
{
    public class FigCustomActionWorker<T> : IHostedService
    {
        private readonly IEnumerable<ICustomAction> _customActions;
        private readonly ILogger<FigCustomActionWorker<T>> _logger;
        private bool _registrationAborted = false;

        public FigCustomActionWorker(IEnumerable<ICustomAction> customActions,
                                           ILogger<FigCustomActionWorker<T>> logger)
        {
            _customActions = customActions;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (_customActions.Any())
            {
                _logger.LogInformation("Starting custom action worker with {Count} actions", _customActions.Count());

                // Register custom actions with the API
                var registrationSucceeded = await RegisterCustomActions();
                if (!registrationSucceeded)
                {
                    _logger.LogError("Aborting custom action worker due to registration failure (possibly due to 404 from API)");
                    _registrationAborted = true;
                    return;
                }

                while (!cancellationToken.IsCancellationRequested && !_registrationAborted)
                {
                    if (CustomActionBridge.PollForCustomActionRequests is not null)
                    {
                        try
                        {
                            var requests = await CustomActionBridge.PollForCustomActionRequests();
                            if (requests != null)
                            {
                                foreach (var request in requests)
                                {
                                    await HandleRequest(request, cancellationToken);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error polling for custom action requests");
                        }
                    }
                    // Wait before polling again
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                }
            }
        }

        private async Task<bool> RegisterCustomActions()
        {
            try
            {
                var actionDefinitions = _customActions.Select(action =>
                    new CustomActionDefinitionDataContract(
                        action.Name,
                        action.ButtonName,
                        action.Description,
                        string.Join(",", action.SettingsUsed))).ToList();

                if (CustomActionBridge.RegisterCustomActions is not null)
                {
                    try
                    {
                        await CustomActionBridge.RegisterCustomActions(actionDefinitions);
                        _logger.LogInformation("Successfully registered {Count} custom actions", actionDefinitions.Count);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        // Check for 404 (NotFound) exception
                        if (ex is System.Net.Http.HttpRequestException httpEx &&
                            httpEx.Message.Contains("404"))
                        {
                            _logger.LogError(ex, "Received 404 Not Found when registering custom actions. The API may be an older version. Aborting registration and polling.");
                            _registrationAborted = true;
                            return false;
                        }
                        _logger.LogError(ex, "Failed to register custom actions");
                        return false;
                    }
                }
                else
                {
                    _logger.LogWarning("CustomActionBridge.RegisterCustomActions is null, cannot register custom actions");
                    return false;
                }
            }
            catch (Exception ex)
            {
                // Check for 404 (NotFound) exception
                if (ex is System.Net.Http.HttpRequestException httpEx &&
                    httpEx.Message.Contains("404"))
                {
                    _logger.LogError(ex, "Received 404 Not Found when registering custom actions. The API may be an older version. Aborting registration and polling.");
                    _registrationAborted = true;
                    return false;
                }
                _logger.LogError(ex, "Failed to register custom actions");
                return false;
            }
        }

        private async Task HandleRequest(CustomActionPollResponseDataContract request, CancellationToken cancellationToken)
        {
            var matchingAction = _customActions.FirstOrDefault(ca => ca.Name == request.CustomActionToExecute);
            if (matchingAction is not null)
            {
                _logger.LogInformation("Executing custom action: {ActionName} with RequestId: {RequestId}", 
                    request.CustomActionToExecute, request.RequestId);
                
                try
                {
                    var result = matchingAction.Execute(cancellationToken);
                    if (CustomActionBridge.SendCustomActionResults is not null)
                    {
                        var results = new List<CustomActionResultDataContract>();
                        await foreach (var a in result)
                        {
                            results.Add(a.ToDataContract());
                        }
                        var executeRequest = new CustomActionExecutionResultsDataContract(request.RequestId, results, results.All(a => a.Succeeded));
                        
                        await CustomActionBridge.SendCustomActionResults(executeRequest);
                        _logger.LogInformation("Successfully sent results for custom action: {ActionName}", request.CustomActionToExecute);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing custom action: {ActionName}", request.CustomActionToExecute);
                    
                    if (CustomActionBridge.SendCustomActionResults is not null)
                    {
                        var executeRequest = new CustomActionExecutionResultsDataContract(request.RequestId, [], false);
                        await CustomActionBridge.SendCustomActionResults(executeRequest);
                    }
                }
            }
            else
            {
                _logger.LogWarning("No matching custom action found for: {ActionName}", request.CustomActionToExecute);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

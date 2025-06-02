using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fig.Common.NetStandard.Json;
using Fig.Contracts.CustomActions;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Integration.Test.Api
{
    [TestFixture]
    public class CustomActionsTests : IntegrationTestBase
    {
        [Test]
        public async Task CanRegisterAndRetrieveCustomActions()
        {
            var secret = GetNewSecret();
            var client = await RegisterSettings<SettingsWithCustomAction>(secret);

            List<CustomActionDefinitionDataContract> actions =
            [
                new("Action1", "Run Test Action", "A simple test action.",
                    "MySetting"),
                new("Action2", "Run DataGrid Action", "Action returning a datagrid.", "MyOtherSetting")
            ];
            
            await RegisterCustomActions(client.ClientName, secret, actions);

            var clients = await GetAllClients();
            var matchingClient = clients.Single();
            
            Assert.That(matchingClient.CustomActions, Is.Not.Null);
            Assert.That(matchingClient.CustomActions.Count, Is.EqualTo(2));

            foreach (var action in actions)
            {
                var matchingAction = matchingClient.CustomActions!.FirstOrDefault(a => a.Name == action.Name);
                Assert.That(matchingAction, Is.Not.Null);
                Assert.That(matchingAction!.ButtonName, Is.EqualTo(action.ButtonName));
                Assert.That(matchingAction.Description, Is.EqualTo(action.Description));
                Assert.That(matchingAction.SettingsUsed, Is.EqualTo(action.SettingsUsed));
            }
        }

        [Test]
        public async Task ShallCompleteFullCustomActionFlow()
        {
            var secret = GetNewSecret();
            var client = await RegisterSettings<SettingsWithCustomAction>(secret);

            List<CustomActionDefinitionDataContract> actions =
            [
                new("Action1", "Run Test Action", "A simple test action.",
                    "MySetting"),
                new("Action2", "Run DataGrid Action", "Action returning a datagrid.", "MyOtherSetting")
            ];

            await RegisterCustomActions(client.ClientName, secret, actions);

            var clients = await GetAllClients();
            var matchingClient = clients.Single();
            
            var action1 = matchingClient.CustomActions.FirstOrDefault(a => a.Name == "Action1");
            Assert.That(action1, Is.Not.Null);
            
            var runSession = Guid.NewGuid();
            var clientStatus = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 5000, true, runSessionId: runSession);
            await GetStatus(client.ClientName, secret, clientStatus);
            
            var emptyResponse = await PollForExecutionRequests(client.ClientName, runSession, secret);
            Assert.That(emptyResponse?.Count(), Is.EqualTo(0));

            var response = await ExecuteAction(client.ClientName, action1!, runSession);
            Assert.That(response, Is.Not.Null);
            
            var submittedStatus = await GetExecutionStatus(response!.ExecutionId);
            Assert.That(submittedStatus?.Status, Is.EqualTo(ExecutionStatus.Submitted));
            
            var pollResponse = (await PollForExecutionRequests(client.ClientName, runSession, secret)).ToList();
            Assert.That(pollResponse.Count, Is.EqualTo(1));
            
            var sentToClientStatus = await GetExecutionStatus(response!.ExecutionId);
            Assert.That(sentToClientStatus?.Status, Is.EqualTo(ExecutionStatus.SentToClient));

            var executionResult = new CustomActionResultDataContract("my result", true) { TextResult = "Result 1" };
            await SubmitActionResult(client.ClientName, secret,
                new CustomActionExecutionResultsDataContract(pollResponse[0].RequestId, [executionResult], true) { RunSessionId = runSession});

            var emptyResponse3 = await PollForExecutionRequests(client.ClientName, runSession, secret);
            Assert.That(emptyResponse3?.Count(), Is.EqualTo(0));
            
            var completedStatus = await GetExecutionStatus(response!.ExecutionId);
            Assert.That(completedStatus?.Status, Is.EqualTo(ExecutionStatus.Completed));
            
            Assert.That(completedStatus!.Results, Is.Not.Null);
            Assert.That(completedStatus.Results!.Count, Is.EqualTo(1));
            Assert.That(completedStatus.Results[0].TextResult, Is.EqualTo(executionResult.TextResult));
            Assert.That(completedStatus.ExecutedByRunSession, Is.EqualTo(runSession));
        }

        [Test]
        public async Task CanUpdateCustomActionsAndRemoveOutdatedOnes()
        {
            var secret = GetNewSecret();
            var client = await RegisterSettings<SettingsWithCustomAction>(secret);

            // Register initial set of custom actions
            List<CustomActionDefinitionDataContract> initialActions =
            [
                new("Action1", "Run Test Action", "A simple test action.", "MySetting"),
                new("Action2", "Run DataGrid Action", "Action returning a datagrid.", "MyOtherSetting"),
                new("Action3", "Legacy Action", "This action will be removed.", "MySetting")
            ];
            
            await RegisterCustomActions(client.ClientName, secret, initialActions);

            var clients = await GetAllClients();
            var matchingClient = clients.Single();
            
            Assert.That(matchingClient.CustomActions, Is.Not.Null);
            Assert.That(matchingClient.CustomActions.Count, Is.EqualTo(3));

            // Update custom actions - modify existing ones and remove Action3, add Action4
            List<CustomActionDefinitionDataContract> updatedActions =
            [
                new("Action1", "Updated Test Action", "An updated test action description.", "MySetting"),
                new("Action2", "Updated DataGrid Action", "Updated action returning a datagrid.", "MyOtherSetting"),
                new("Action4", "New Action", "A newly added action.", "MySetting")
            ];
            
            // This should trigger the deletion of Action3 and update of Action1 and Action2
            await RegisterCustomActions(client.ClientName, secret, updatedActions);

            // Verify the actions were updated correctly
            var updatedClients = await GetAllClients();
            var updatedMatchingClient = updatedClients.Single();
            
            Assert.That(updatedMatchingClient.CustomActions, Is.Not.Null);
            Assert.That(updatedMatchingClient.CustomActions.Count, Is.EqualTo(3));

            // Verify Action3 was removed
            var action3 = updatedMatchingClient.CustomActions!.FirstOrDefault(a => a.Name == "Action3");
            Assert.That(action3, Is.Null, "Action3 should have been removed");

            // Verify Action1 was updated
            var action1 = updatedMatchingClient.CustomActions!.FirstOrDefault(a => a.Name == "Action1");
            Assert.That(action1, Is.Not.Null);
            Assert.That(action1!.ButtonName, Is.EqualTo("Updated Test Action"));
            Assert.That(action1.Description, Is.EqualTo("An updated test action description."));

            // Verify Action2 was updated
            var action2 = updatedMatchingClient.CustomActions!.FirstOrDefault(a => a.Name == "Action2");
            Assert.That(action2, Is.Not.Null);
            Assert.That(action2!.ButtonName, Is.EqualTo("Updated DataGrid Action"));
            Assert.That(action2.Description, Is.EqualTo("Updated action returning a datagrid."));

            // Verify Action4 was added
            var action4 = updatedMatchingClient.CustomActions!.FirstOrDefault(a => a.Name == "Action4");
            Assert.That(action4, Is.Not.Null);
            Assert.That(action4!.ButtonName, Is.EqualTo("New Action"));
            Assert.That(action4.Description, Is.EqualTo("A newly added action."));
        }

        [Test]
        public async Task CanCompleteCustomActionFlowAfterUpdatingActions()
        {
            var secret = GetNewSecret();
            var client = await RegisterSettings<SettingsWithCustomAction>(secret);

            // Register initial actions
            List<CustomActionDefinitionDataContract> initialActions =
            [
                new("Action1", "Run Test Action", "A simple test action.", "MySetting"),
                new("ActionToRemove", "Legacy Action", "This will be removed.", "MySetting")
            ];
            
            await RegisterCustomActions(client.ClientName, secret, initialActions);

            // Update actions - remove one, update one, add one
            List<CustomActionDefinitionDataContract> updatedActions =
            [
                new("Action1", "Updated Test Action", "Updated description.", "MySetting"),
                new("NewAction", "Brand New Action", "A newly added action.", "MySetting")
            ];
            
            await RegisterCustomActions(client.ClientName, secret, updatedActions);

            var clients = await GetAllClients();
            var matchingClient = clients.Single();
            
            Assert.That(matchingClient.CustomActions, Is.Not.Null);
            Assert.That(matchingClient.CustomActions.Count, Is.EqualTo(2));
            
            // Verify we can execute the updated action
            var action1 = matchingClient.CustomActions.FirstOrDefault(a => a.Name == "Action1");
            Assert.That(action1, Is.Not.Null);
            Assert.That(action1!.ButtonName, Is.EqualTo("Updated Test Action"));
            
            var runSession = Guid.NewGuid();
            var clientStatus = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 5000, true, runSessionId: runSession);
            await GetStatus(client.ClientName, secret, clientStatus);
            
            // Execute the updated action
            var response = await ExecuteAction(client.ClientName, action1!, runSession);
            Assert.That(response, Is.Not.Null);
            
            var submittedStatus = await GetExecutionStatus(response!.ExecutionId);
            Assert.That(submittedStatus?.Status, Is.EqualTo(ExecutionStatus.Submitted));
            
            var pollResponse = (await PollForExecutionRequests(client.ClientName, runSession, secret)).ToList();
            Assert.That(pollResponse.Count, Is.EqualTo(1));
            
            var sentToClientStatus = await GetExecutionStatus(response!.ExecutionId);
            Assert.That(sentToClientStatus?.Status, Is.EqualTo(ExecutionStatus.SentToClient));

            // Complete the execution
            var executionResult = new CustomActionResultDataContract("updated action result", true) { TextResult = "Updated Action Result" };
            await SubmitActionResult(client.ClientName, secret,
                new CustomActionExecutionResultsDataContract(pollResponse[0].RequestId, [executionResult], true) { RunSessionId = runSession});

            var completedStatus = await GetExecutionStatus(response!.ExecutionId);
            Assert.That(completedStatus?.Status, Is.EqualTo(ExecutionStatus.Completed));
            Assert.That(completedStatus!.Results![0].TextResult, Is.EqualTo("Updated Action Result"));
        }

        [Test]
        public async Task ShallRejectRegistrationWithInvalidClientSecret()
        {
            var secret = GetNewSecret();
            var client = await RegisterSettings<SettingsWithCustomAction>(secret);

            List<CustomActionDefinitionDataContract> actions =
            [
                new("Action1", "Test Action", "A test action.", "MySetting")
            ];
            
            var response = await RegisterCustomActions(client.ClientName, "invalid-secret", actions, validateSuccess: false);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public async Task ShallRejectRegistrationForNonExistentClient()
        {
            var secret = GetNewSecret();

            List<CustomActionDefinitionDataContract> actions =
            [
                new("Action1", "Test Action", "A test action.", "MySetting")
            ];

            var response = await RegisterCustomActions("non existent client", secret, actions, validateSuccess: false);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task ShallHandleEmptyCustomActionsList()
        {
            var secret = GetNewSecret();
            var client = await RegisterSettings<SettingsWithCustomAction>(secret);

            // First register some actions
            List<CustomActionDefinitionDataContract> initialActions =
            [
                new("Action1", "Test Action", "A test action.", "MySetting")
            ];
            
            await RegisterCustomActions(client.ClientName, secret, initialActions);

            // Verify action was registered
            var clients = await GetAllClients();
            var matchingClient = clients.Single();
            Assert.That(matchingClient.CustomActions?.Count, Is.EqualTo(1));

            // Now register empty list - should remove all actions
            await RegisterCustomActions(client.ClientName, secret, []);

            var updatedClients = await GetAllClients();
            var updatedMatchingClient = updatedClients.Single();
            Assert.That(updatedMatchingClient.CustomActions?.Count ?? 0, Is.EqualTo(0));
        }

        [Test]
        public async Task ShallRejectExecutionForNonExistentAction()
        {
            var secret = GetNewSecret();
            var client = await RegisterSettings<SettingsWithCustomAction>(secret);

            List<CustomActionDefinitionDataContract> actions =
            [
                new("Action1", "Test Action", "A test action.", "MySetting")
            ];
            
            await RegisterCustomActions(client.ClientName, secret, actions);

            var result = await ExecuteAction(client.ClientName,
                new CustomActionDefinitionDataContract("NonExistentAction",
                    "Button",
                    "Description",
                    "Setting"));
            
            Assert.That(result?.ExecutionPending, Is.False);
            Assert.That(result?.Message, Does.Contain("did not exist"));
        }

        [Test]
        public void ShallRejectExecutionStatusForInvalidExecutionId()
        {
            var invalidId = Guid.NewGuid();
            
            Assert.ThrowsAsync<HttpRequestException>(() =>
                GetExecutionStatus(invalidId));
        }

        [Test]
        public async Task ShallReturnEmptyHistoryForNonExistentAction()
        {
            var secret = GetNewSecret();
            var client = await RegisterSettings<SettingsWithCustomAction>(secret);

            var startTime = DateTime.UtcNow.AddDays(-1);
            var endTime = DateTime.UtcNow;
            
            var history = await GetExecutionHistory(client.ClientName, "NonExistentAction", startTime, endTime);
            
            Assert.That(history, Is.Not.Null);
            Assert.That(history!.Executions, Is.Empty);
        }

        /*
        [Test]
        public async Task ShallFilterExecutionHistoryByDateRange()
        {
            var secret = GetNewSecret();
            var client = await RegisterSettings<SettingsWithCustomAction>(secret);

            List<CustomActionDefinitionDataContract> actions =
            [
                new("Action1", "Test Action", "A test action.", "MySetting")
            ];
            
            await RegisterCustomActions(client.ClientName, secret, actions);

            var runSession = Guid.NewGuid();
            var clientStatus = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 5000, true, runSessionId: runSession);
            await GetStatus(client.ClientName, secret, clientStatus);

            // Execute action
            var beforeExecution = DateTime.UtcNow;
            var response = await ExecuteAction(client.ClientName, actions[0], runSession);
            var afterExecution = DateTime.UtcNow;

            // Complete execution
            var pollResponse = (await PollForExecutionRequests(client.ClientName, runSession, secret)).ToList();
            var executionResult = new CustomActionResultDataContract("result") { TextResult = "Test Result" };
            await SubmitActionResult(client.ClientName, secret,
                new CustomActionExecutionResultsDataContract(pollResponse[0].RequestId, [executionResult], true) { RunSessionId = runSession});

            // Get history with date range that includes the execution
            var historyInRange = await GetExecutionHistory(client.ClientName, actions[0].Name, beforeExecution, afterExecution);
            Assert.That(historyInRange!.Executions.Count, Is.EqualTo(1));

            // Get history with date range that excludes the execution
            var historyOutOfRange = await GetExecutionHistory(client.ClientName, actions[0].Name, beforeExecution.AddDays(-1), beforeExecution.AddMinutes(-1));
            Assert.That(historyOutOfRange!.Executions.Count, Is.EqualTo(0));
        }
*/
        [Test]
        public async Task ShallHandleClientSideConfigurationProviderWithMultipleCustomActions()
        {
            var secret = GetNewSecret();
            var client = await RegisterSettings<SettingsWithCustomAction>(secret);

            // Register multiple custom actions
            List<CustomActionDefinitionDataContract> actions =
            [
                new("Action1", "First Action", "First test action.", "MySetting"),
                new("Action2", "Second Action", "Second test action.", "MySetting"),
                new("Action3", "Third Action", "Third test action.", "MySetting")
            ];
            
            await RegisterCustomActions(client.ClientName, secret, actions);

            // Initialize configuration provider (client-side)
            var (options, configuration) = InitializeConfigurationProvider<SettingsWithCustomAction>(secret);
            
            // Verify the configuration provider can access the settings
            var settings = options.CurrentValue;
            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.MySetting, Is.EqualTo("DefaultValue"));
            Assert.That(settings.ClientName, Is.EqualTo("SettingsWithCustomAction"));

            // Verify the custom actions are properly registered server-side
            var clients = await GetAllClients();
            var matchingClient = clients.Single();
            
            Assert.That(matchingClient.CustomActions, Is.Not.Null);
            Assert.That(matchingClient.CustomActions.Count, Is.EqualTo(3));

            // Verify all actions are present
            var actionNames = matchingClient.CustomActions!.Select(a => a.Name).ToList();
            Assert.That(actionNames, Does.Contain("Action1"));
            Assert.That(actionNames, Does.Contain("Action2"));
            Assert.That(actionNames, Does.Contain("Action3"));

            // Test that we can execute actions while client is connected
            var runSession = Guid.NewGuid();
            var clientStatus = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 5000, true, runSessionId: runSession);
            await GetStatus(client.ClientName, secret, clientStatus);

            // Execute one of the actions
            var response = await ExecuteAction(client.ClientName, actions[1], runSession); // Action2
            Assert.That(response, Is.Not.Null);

            var pollResponse = (await PollForExecutionRequests(client.ClientName, runSession, secret)).ToList();
            Assert.That(pollResponse.Count, Is.EqualTo(1));
            Assert.That(pollResponse[0].CustomActionToExecute, Is.EqualTo("Action2"));
        }

        [Test]
        public async Task ShallHandleConcurrentCustomActionExecutions()
        {
            var secret = GetNewSecret();
            var client = await RegisterSettings<SettingsWithCustomAction>(secret);

            List<CustomActionDefinitionDataContract> actions =
            [
                new("Action1", "Concurrent Action 1", "First concurrent action.", "MySetting"),
                new("Action2", "Concurrent Action 2", "Second concurrent action.", "MySetting")
            ];
            
            await RegisterCustomActions(client.ClientName, secret, actions);

            var runSession = Guid.NewGuid();
            var clientStatus = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 5000, true, runSessionId: runSession);
            await GetStatus(client.ClientName, secret, clientStatus);

            // Execute both actions concurrently
            var response1Task = ExecuteAction(client.ClientName, actions[0], runSession);
            var response2Task = ExecuteAction(client.ClientName, actions[1], runSession);

            var responses = await Task.WhenAll(response1Task, response2Task);
            
            Assert.That(responses[0], Is.Not.Null);
            Assert.That(responses[1], Is.Not.Null);
            Assert.That(responses[0]!.ExecutionId, Is.Not.EqualTo(responses[1]!.ExecutionId));

            // Both should show up in poll
            var pollResponse = (await PollForExecutionRequests(client.ClientName, runSession, secret)).ToList();
            Assert.That(pollResponse.Count, Is.EqualTo(2));

            var actionNames = pollResponse.Select(p => p.CustomActionToExecute).ToList();
            Assert.That(actionNames, Does.Contain("Action1"));
            Assert.That(actionNames, Does.Contain("Action2"));
        }

        [Test]
        public async Task ShallHandleCustomActionWithSpecialCharactersInName()
        {
            var secret = GetNewSecret();
            var client = await RegisterSettings<SettingsWithCustomAction>(secret);

            List<CustomActionDefinitionDataContract> actions =
            [
                new("Action With Spaces", "Action Button", "Action with spaces in name.", "MySetting"),
                new("Action-With-Dashes", "Dash Action", "Action with dashes.", "MySetting"),
                new("Action_With_Underscores", "Underscore Action", "Action with underscores.", "MySetting")
            ];
            
            await RegisterCustomActions(client.ClientName, secret, actions);

            var clients = await GetAllClients();
            var matchingClient = clients.Single();
            
            Assert.That(matchingClient.CustomActions, Is.Not.Null);
            Assert.That(matchingClient.CustomActions.Count, Is.EqualTo(3));

            // Test execution of action with spaces
            var runSession = Guid.NewGuid();
            var clientStatus = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 5000, true, runSessionId: runSession);
            await GetStatus(client.ClientName, secret, clientStatus);

            var response = await ExecuteAction(client.ClientName, actions[0], runSession); // Action with spaces
            Assert.That(response, Is.Not.Null);

            var pollResponse = (await PollForExecutionRequests(client.ClientName, runSession, secret)).ToList();
            Assert.That(pollResponse.Count, Is.EqualTo(1));
            Assert.That(pollResponse[0].CustomActionToExecute, Is.EqualTo("Action With Spaces"));
        }

        [Test]
        public async Task ShallHandleCustomActionExecutionTimeout()
        {
            var secret = GetNewSecret();
            var client = await RegisterSettings<SettingsWithCustomAction>(secret);

            List<CustomActionDefinitionDataContract> actions =
            [
                new("TimeoutAction", "Timeout Action", "Action that will timeout.", "MySetting")
            ];
            
            await RegisterCustomActions(client.ClientName, secret, actions);

            var runSession = Guid.NewGuid();
            var clientStatus = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 5000, true, runSessionId: runSession);
            await GetStatus(client.ClientName, secret, clientStatus);

            var response = await ExecuteAction(client.ClientName, actions[0], runSession);
            Assert.That(response, Is.Not.Null);

            // Poll for the request
            var pollResponse = (await PollForExecutionRequests(client.ClientName, runSession, secret)).ToList();
            Assert.That(pollResponse.Count, Is.EqualTo(1));

            // Don't submit result - just check status remains as SentToClient
            await Task.Delay(100); // Small delay to ensure status is updated

            var status = await GetExecutionStatus(response!.ExecutionId);
            Assert.That(status!.Status, Is.EqualTo(ExecutionStatus.SentToClient));
            Assert.That(status.Results, Is.Null);
        }

        private async Task<CustomActionExecutionHistoryDataContract?> GetExecutionHistory(string clientName, string customActionName, DateTime startTime, DateTime endTime)
        {
            var uri = $"customactions/history/{Uri.EscapeDataString(clientName)}/{Uri.EscapeDataString(customActionName)}";
            uri += $"?startTime={startTime:yyyy-MM-ddTHH:mm:ss.fffZ}&endTime={endTime:yyyy-MM-ddTHH:mm:ss.fffZ}";

            var result = await ApiClient.Get<CustomActionExecutionHistoryDataContract>(uri);
            return result;
        }

        private async Task<HttpResponseMessage> RegisterCustomActions(string clientName, string secret, IEnumerable<CustomActionDefinitionDataContract> customActions, bool validateSuccess = true)
        {
            var request = new CustomActionRegistrationRequestDataContract(clientName, customActions.ToList());
            return await ApiClient.Post("customactions/register", request, secret, validateSuccess: validateSuccess);
        }

        private async Task<CustomActionExecutionResponseDataContract?> ExecuteAction(string clientName, CustomActionDefinitionDataContract action, Guid? runSessionId = null, bool validateSuccess = true)
        {
            var request = new CustomActionExecutionRequestDataContract(action.Name, runSessionId ?? Guid.NewGuid());
            var uri = $"customactions/execute/{Uri.EscapeDataString(clientName)}";
            return await ApiClient.Put<CustomActionExecutionResponseDataContract>(uri, request, authenticate: true, validateSuccess: validateSuccess);
        }

        private async Task<CustomActionExecutionStatusDataContract?> GetExecutionStatus(Guid executionId)
        {
            var uri = $"customactions/status/{executionId}";
            return await ApiClient.Get<CustomActionExecutionStatusDataContract>(uri, authenticate: true);
        }

        private async Task<IEnumerable<CustomActionPollResponseDataContract>> PollForExecutionRequests(string clientName, Guid runSession, string clientSecret)
        {
            var uri = $"customactions/poll/{Uri.EscapeDataString(clientName)}?runSessionId={runSession}";
            using var httpClient = GetHttpClient();
            httpClient.DefaultRequestHeaders.Add("clientSecret", clientSecret);
            var response = await httpClient.GetAsync(uri);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<IEnumerable<CustomActionPollResponseDataContract>>(content, JsonSettings.FigDefault);
            return result ?? [];
        }

        private async Task SubmitActionResult(string clientName, string secret, CustomActionExecutionResultsDataContract result)
        {
            var uri = $"customactions/results/{Uri.EscapeDataString(clientName)}";
            await ApiClient.Post(uri, result, secret);
        }
    }
}

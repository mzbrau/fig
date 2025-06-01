using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fig.Contracts.CustomActions;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
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
            var status = await GetStatus(client.ClientName, secret, clientStatus);
            
            var emptyResponse = await PollForExecutionRequests(client.ClientName, runSession, secret);
            Assert.That(emptyResponse?.Count(), Is.EqualTo(0));

            var response = await ExecuteAction(client.ClientName, action1!);
            Assert.That(response, Is.Not.Null);
            
            var submittedStatus = await GetExecutionStatus(response!.ExecutionId);
            Assert.That(submittedStatus?.Status, Is.EqualTo(ExecutionStatus.Submitted));
            
            
            var pollResponse = (await PollForExecutionRequests(client.ClientName, runSession, secret)).ToList();
            Assert.That(pollResponse.Count, Is.EqualTo(1));
            
            var sentToClientStatus = await GetExecutionStatus(response!.ExecutionId);
            Assert.That(sentToClientStatus?.Status, Is.EqualTo(ExecutionStatus.SentToClient));

            var executionResult = new CustomActionResultDataContract("my result") { TextResult = "Result 1" };
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

        private async Task<CustomActionExecutionStatusDataContract> GetExecutionStatus(Guid executionId)
        {
            var response = await ApiClient.Get<CustomActionExecutionStatusDataContract>($"customactions/status/{executionId}");
            if (response is null)
                Assert.Fail("Failed to get execution status");

            return response!;
        }

        private async Task SubmitActionResult(string clientName, string secret, CustomActionExecutionResultsDataContract customActionExecuteRequestDataContract)
        {
            await ApiClient.Post($"customactions/results/{Uri.EscapeDataString(clientName)}",
                customActionExecuteRequestDataContract, secret);
        }

        private async Task<IEnumerable<CustomActionPollResponseDataContract>> PollForExecutionRequests(string clientClientName, Guid runSession, string clientSecret)
        {
            var uri = $"customactions/poll/{Uri.EscapeDataString(clientClientName)}";
            uri += $"?runSessionId={runSession}";

            var result = await ApiClient.Get<IEnumerable<CustomActionPollResponseDataContract>>(uri, secret: clientSecret);
            if (result is null)
                Assert.Fail("Failed to poll for execution requests");

            return result!;
        }

        private async Task<CustomActionExecutionResponseDataContract?> ExecuteAction(string clientName, CustomActionDefinitionDataContract action1)
        {
            return await ApiClient.Put<CustomActionExecutionResponseDataContract>($"customactions/execute/{Uri.EscapeDataString(clientName)}",
                new CustomActionExecutionRequestDataContract(action1.Name));
        }


        private async Task RegisterCustomActions(string clientName, string secret, IEnumerable<CustomActionDefinitionDataContract> customActions)
        {
            await ApiClient.Post("customactions/register",
                new CustomActionRegistrationRequestDataContract(clientName, customActions.ToList()), secret);
        }
    }
}

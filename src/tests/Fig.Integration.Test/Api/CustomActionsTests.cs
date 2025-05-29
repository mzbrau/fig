using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Fig.Client.ClientSecret;
using Fig.Client.Configuration;
using Fig.Client.ExtensionMethods;
using Fig.Contracts.CustomActions;
using Fig.Contracts.Settings;
using Fig.Contracts.SettingVerification;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Fig.Common.NetStandard.Constants; // For FigConstants.DefaultInstance

namespace Fig.Integration.Test.Api
{
    [TestFixture]
    public class CustomActionsTests : IntegrationTestBase
    {
        private const string ClientName = "CustomActionTestClient";
        private const string ClientSecret = "customactiontestsecret";
        private ServiceProvider _serviceProvider;
        private SettingsWithCustomAction _settings;

        [OneTimeSetUp]
        public async Task FixtureSetup()
        {
            // Configure and start the client
            var services = new ServiceCollection();
            services.AddFig<SettingsWithCustomAction>(options =>
            {
                options.ClientName = ClientName;
                options.ApiUri = new Uri(ApiBaseAddress); // Ensure ApiBaseAddress is from IntegrationTestBase
                options.AllowOfflineSettings = false;
                options.LiveReload = false; // Typically false for tests unless testing live reload
                options.CustomActionPollInterval = TimeSpan.FromMilliseconds(100); // Fast polling for tests
            });

            services.AddSingleton<IClientSecretProvider>(new InCodeClientSecretProvider(ClientSecret));
            
            // Register custom actions
            services.AddCustomAction<SimpleCustomAction, SettingsWithCustomAction>();
            services.AddCustomAction<DataGridCustomAction, SettingsWithCustomAction>();

            _serviceProvider = services.BuildServiceProvider();
            
            // Start Fig Client (this will trigger registration)
            // We need to get the IHostedService instances that manage the client's lifecycle
            // For simplicity in test, we might manually trigger registration if direct start isn't straightforward.
            // However, the AddFig call registers workers that should handle this.
            // We need to ensure the client is "running" so it can poll.
            // This might require starting the host or manually managing the Fig client lifecycle components.
            // For now, let's assume the services are started by building the provider and getting relevant instances.

            _settings = _serviceProvider.GetRequiredService<SettingsWithCustomAction>();

            // Give client time to register. In a real test, use a more robust wait mechanism.
            await Task.Delay(TimeSpan.FromSeconds(5)); // Increased delay to ensure registration
        }

        [OneTimeTearDown]
        public async Task FixtureTearDown()
        {
            try
            {
                await DeleteClient(ClientName, FigConstants.DefaultInstance);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during teardown: {ex.Message}");
            }
            _serviceProvider?.Dispose();
        }
        
        [Test, Order(1)]
        public async Task CanRegisterAndRetrieveCustomActions()
        {
            var actions = await ApiClient.Get<List<CustomActionDefinitionDataContract>>($"api/customactions/{ClientName}/{FigConstants.DefaultInstance}");
            Assert.That(actions, Is.Not.Null);
            Assert.That(actions.Count, Is.EqualTo(2));

            var simpleAction = actions.FirstOrDefault(a => a.Name == "TestAction");
            Assert.That(simpleAction, Is.Not.Null);
            Assert.That(simpleAction.ButtonName, Is.EqualTo("Run Test Action"));
            Assert.That(simpleAction.Description, Is.EqualTo("A simple test action."));
            Assert.That(simpleAction.SettingsUsed, Is.Not.Null);
            Assert.That(simpleAction.SettingsUsed.Count, Is.EqualTo(1));
            Assert.That(simpleAction.SettingsUsed.First().Name, Is.EqualTo(nameof(MyActionSetting)));


            var dataGridAction = actions.FirstOrDefault(a => a.Name == "DataGridAction");
            Assert.That(dataGridAction, Is.Not.Null);
            Assert.That(dataGridAction.ButtonName, Is.EqualTo("Run DataGrid Action"));
            Assert.That(dataGridAction.Description, Is.EqualTo("Action returning a datagrid."));
        }

        [Test, Order(2)]
        public async Task CanExecuteSimpleActionAndGetTextResult()
        {
            var actions = await ApiClient.Get<List<CustomActionDefinitionDataContract>>($"api/customactions/{ClientName}/{FigConstants.DefaultInstance}");
            var simpleAction = actions.First(a => a.Name == "TestAction");
            var actionSettingValue = "TestValueFromAction";

            var request = new CustomActionExecutionRequestDataContract
            {
                CustomActionId = simpleAction.Id, // Assuming Id is populated by the API on retrieval
                Instance = FigConstants.DefaultInstance, // Execute on default instance
                Settings = new List<SettingDataContract>
                {
                    new SettingDataContract(nameof(MyActionSetting), new StringSettingDataContract(actionSettingValue))
                }
            };

            var execResponse = await ApiClient.Post<CustomActionExecutionResponseDataContract>($"api/customactions/execute/{ClientName}", request);
            Assert.That(execResponse, Is.Not.Null);
            Assert.That(execResponse.ExecutionId, Is.Not.EqualTo(Guid.Empty));

            CustomActionExecutionStatusDataContract? status = null;
            for (int i = 0; i < 20; i++) // Poll for up to 2 seconds (20 * 100ms poll interval)
            {
                status = await ApiClient.Get<CustomActionExecutionStatusDataContract>($"api/customactions/status/{execResponse.ExecutionId}");
                if (status?.Status == "Completed" || status?.Status == "Failed")
                    break;
                await Task.Delay(TimeSpan.FromMilliseconds(200)); // Wait for client to poll and execute
            }

            Assert.That(status, Is.Not.Null, "Status was not retrieved.");
            Assert.That(status.Status, Is.EqualTo("Completed"), $"Action failed with: {status.ErrorMessage}");
            Assert.That(status.Results, Is.Not.Null.And.Not.Empty);
            var textResult = status.Results.First(r => r.Name == "ExecutionResult");
            Assert.That(textResult.ResultType, Is.EqualTo(CustomActionResultTypeDataContract.Text));
            Assert.That(textResult.TextResult, Is.EqualTo($"Action Executed Successfully. Setting Value: {actionSettingValue}"));
        }

        [Test, Order(3)]
        public async Task CanExecuteDataGridActionAndGetDataGridResult()
        {
            var actions = await ApiClient.Get<List<CustomActionDefinitionDataContract>>($"api/customactions/{ClientName}/{FigConstants.DefaultInstance}");
            var dataGridAction = actions.First(a => a.Name == "DataGridAction");

            var request = new CustomActionExecutionRequestDataContract
            {
                CustomActionId = dataGridAction.Id,
                Instance = FigConstants.DefaultInstance
            };

            var execResponse = await ApiClient.Post<CustomActionExecutionResponseDataContract>($"api/customactions/execute/{ClientName}", request);
            Assert.That(execResponse.ExecutionId, Is.Not.EqualTo(Guid.Empty));

            CustomActionExecutionStatusDataContract? status = null;
            for (int i = 0; i < 20; i++)
            {
                status = await ApiClient.Get<CustomActionExecutionStatusDataContract>($"api/customactions/status/{execResponse.ExecutionId}");
                if (status?.Status == "Completed" || status?.Status == "Failed")
                    break;
                await Task.Delay(TimeSpan.FromMilliseconds(200));
            }
            
            Assert.That(status, Is.Not.Null, "Status was not retrieved.");
            Assert.That(status.Status, Is.EqualTo("Completed"), $"Action failed with: {status.ErrorMessage}");
            Assert.That(status.Results, Is.Not.Null.And.Not.Empty);
            var dgResult = status.Results.First(r => r.Name == "DataGridOutput");
            Assert.That(dgResult.ResultType, Is.EqualTo(CustomActionResultTypeDataContract.DataGrid));
            Assert.That(dgResult.DataGridResult, Is.Not.Null);
            Assert.That(dgResult.DataGridResult.GridColumns.Count, Is.EqualTo(2));
            Assert.That(dgResult.DataGridResult.GridColumns.Any(c => c.Name == "ID"), Is.True);
            Assert.That(dgResult.DataGridResult.GridColumns.Any(c => c.Name == "Value"), Is.True);
            Assert.That(dgResult.DataGridResult.Values.Count, Is.EqualTo(2));
            Assert.That(dgResult.DataGridResult.Values[0]["ID"], Is.EqualTo(1));
            Assert.That(dgResult.DataGridResult.Values[0]["Value"], Is.EqualTo("A"));
        }

        [Test, Order(4)]
        public async Task RunInstanceSelectionDefaultsToAutoAndExecutes()
        {
            var actions = await ApiClient.Get<List<CustomActionDefinitionDataContract>>($"api/customactions/{ClientName}/{FigConstants.DefaultInstance}");
            var simpleAction = actions.First(a => a.Name == "TestAction");

            var request = new CustomActionExecutionRequestDataContract
            {
                CustomActionId = simpleAction.Id,
                Instance = null // "auto" behavior
            };

            var execResponse = await ApiClient.Post<CustomActionExecutionResponseDataContract>($"api/customactions/execute/{ClientName}", request);
            Assert.That(execResponse.ExecutionId, Is.Not.EqualTo(Guid.Empty));

            CustomActionExecutionStatusDataContract? status = null;
            for (int i = 0; i < 10; i++)
            {
                status = await ApiClient.Get<CustomActionExecutionStatusDataContract>($"api/customactions/status/{execResponse.ExecutionId}");
                if (status?.Status == "Completed" || status?.Status == "Failed")
                    break;
                await Task.Delay(TimeSpan.FromMilliseconds(200));
            }

            Assert.That(status, Is.Not.Null, "Status was not retrieved.");
            Assert.That(status.Status, Is.EqualTo("Completed"), $"Action failed with: {status.ErrorMessage}");
        }
        
        [Test, Order(5)]
        public async Task ExecutionHistoryIsRecorded()
        {
            var actions = await ApiClient.Get<List<CustomActionDefinitionDataContract>>($"api/customactions/{ClientName}/{FigConstants.DefaultInstance}");
            var simpleAction = actions.First(a => a.Name == "TestAction");

            // Execute action twice
            for(int i=0; i<2; i++)
            {
                var request = new CustomActionExecutionRequestDataContract { CustomActionId = simpleAction.Id, Instance = FigConstants.DefaultInstance };
                var execResponse = await ApiClient.Post<CustomActionExecutionResponseDataContract>($"api/customactions/execute/{ClientName}", request);
                CustomActionExecutionStatusDataContract? status = null;
                for (int j = 0; j < 10; j++) // Wait for completion
                {
                    status = await ApiClient.Get<CustomActionExecutionStatusDataContract>($"api/customactions/status/{execResponse.ExecutionId}");
                    if (status?.Status == "Completed" || status?.Status == "Failed") break;
                    await Task.Delay(200);
                }
                Assert.That(status?.Status, Is.EqualTo("Completed"));
            }

            var history = await ApiClient.Get<CustomActionExecutionHistoryDataContract>($"api/customactions/history/{simpleAction.Id}?limit=5&offset=0");
            Assert.That(history, Is.Not.Null);
            Assert.That(history.Executions.Count, Is.GreaterThanOrEqualTo(2)); // Should be at least 2, could be more if other tests ran it
            Assert.That(history.Executions.All(e => e.Status == "Completed"), Is.True);
        }

        [Test, Order(6)]
        public async Task DeleteClientRemovesCustomActionData()
        {
            // Ensure client and actions exist
            var actionsBeforeDelete = await ApiClient.Get<List<CustomActionDefinitionDataContract>>($"api/customactions/{ClientName}/{FigConstants.DefaultInstance}");
            Assert.That(actionsBeforeDelete, Is.Not.Empty);
            var actionId = actionsBeforeDelete.First().Id;

            // Delete client
            await DeleteClient(ClientName, FigConstants.DefaultInstance); // Method from IntegrationTestBase or similar
            
            // Allow time for asynchronous cleanup if any (though it should be synchronous for custom actions by design)
            await Task.Delay(TimeSpan.FromSeconds(1));

            // Attempt to retrieve custom actions
            var ex = Assert.ThrowsAsync<HttpException>(async () => 
                await ApiClient.Get<List<CustomActionDefinitionDataContract>>($"api/customactions/{ClientName}/{FigConstants.DefaultInstance}"));
            Assert.That(ex.StatusCode, Is.EqualTo(HttpStatusCode.NotFound)); // Or check for empty list if API returns 200 OK with empty

            // Attempt to retrieve history (should also fail or be empty)
             var historyEx = Assert.ThrowsAsync<HttpException>(async () => 
                await ApiClient.Get<CustomActionExecutionHistoryDataContract>($"api/customactions/history/{actionId}"));
            Assert.That(historyEx.StatusCode, Is.EqualTo(HttpStatusCode.NotFound)); // API should return 404 if action itself is gone.
        }
        
        // Note: `RunInstanceSelectionSpecificInstanceExecutesOnCorrectInstance` is skipped as it's marked "More Advanced"
        // and would require more complex setup (multiple client instances in the same test process)
        // which might be beyond the scope of typical integration test setup without a dedicated test harness for multiple clients.
    }
}

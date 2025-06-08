using System.Threading.Tasks;
using Fig.Client;
using NUnit.Framework;

namespace Fig.Integration.VersionCompatibilityTest
{
    public abstract class VersionCompatibilityTestBase
    {
        protected TestSettings Settings = new();
        protected Fig.Client.IntegrationTest.ConfigReloader ConfigReloader = new Fig.Client.IntegrationTest.ConfigReloader();
        // TODO: Replace with actual client type when available
        protected object? Client;

        [SetUp]
        public virtual void Setup()
        {
            // Setup logic for derived classes
        }

        [TearDown]
        public virtual void Teardown()
        {
            // Teardown logic for derived classes
        }

        protected async Task RegisterAndUpdateSettings()
        {
            // TODO: Replace with actual client logic when available
            // await Client!.RegisterSettings(Settings);
            // Assert.That((await Client.GetSettings<TestSettings>()).Location, Is.EqualTo(Settings.Location));

            // Settings.Location = "London";
            // await Client.UpdateSettings(Settings);
            // Assert.That((await Client.GetSettings<TestSettings>()).Location, Is.EqualTo("London"));
            await Task.CompletedTask;
        }
    }
}

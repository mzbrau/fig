using NUnit.Framework;
using System.Threading.Tasks;
using Fig.Client; // For latest client
// using Fig.Client; // For 1.2.0 client, will be handled by build config
using System.Net.Http;
using System;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Fig.Integration.VersionCompatibilityTest
{

    [TestFixture]
    [Category("OldClient")]
    public class OldClientAgainstLatestApiTests : VersionCompatibilityTestBase
    {
        [SetUp]
        public override void Setup()
        {
            // Use Fig.Client 1.2.0 (handled by build config)
            // Setup Client to point to latest API
            // Example: Client = new FigClient("http://localhost:5000");
        }

        [Test]
        public async Task Should_Register_And_Update_Settings_With_Old_Client()
        {
            await RegisterAndUpdateSettings();
        }
    }

    [TestFixture]
    [Category("LatestClient")]
    public class LatestClientAgainstOldApiTests : VersionCompatibilityTestBase
    {
        private TestcontainersContainer _figApiContainer;
        private string _apiUrl = "http://localhost:5000";

        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            _figApiContainer = new TestcontainersBuilder<TestcontainersContainer>()
                .WithImage("figapi:1.2.0")
                .WithPortBinding(5000, 5000)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5000))
                .Build();
            await _figApiContainer.StartAsync();
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            await _figApiContainer.StopAsync();
        }

        [SetUp]
        public override void Setup()
        {
            // Use latest Fig.Client
            // Setup Client to point to 1.2.0 API
            // Example: Client = new FigClient(_apiUrl);
        }

        [Test]
        public async Task Should_Register_And_Update_Settings_With_Latest_Client()
        {
            await RegisterAndUpdateSettings();
        }
    }
}

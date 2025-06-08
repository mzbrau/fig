using NUnit.Framework;

namespace Fig.Integration.VersionCompatibilityTest
{
    [SetUpFixture]
    public class TestSetup
    {
        [OneTimeSetUp]
        public void GlobalSetup()
        {
            // Global setup for integration tests
        }

        [OneTimeTearDown]
        public void GlobalTeardown()
        {
            // Global teardown for integration tests
        }
    }
}

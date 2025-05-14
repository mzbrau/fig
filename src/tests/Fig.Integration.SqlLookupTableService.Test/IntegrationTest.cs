using Moq;

namespace Fig.Integration.SqlLookupTableService.Test
{
    [TestFixture]
    public class IntegrationTest : IntegrationTestBase
    {
        [Test]
        public void ShallLogIntoFig()
        {
            FigFacadeMock.Verify(a => a.Login(), Times.Once);
        }

        [Test]
        public void ShallExecuteSqlQuery()
        {
            SqlQueryManagerMock.Verify(a => a.ExecuteQuery("SELECT 1"), Times.Once);
        }

        [Test]
        public async Task ShallReloadConfiguration()
        {
            Settings.Configuration =
            [
                new()
                {
                    Name = "Test",
                    SqlExpression = "SELECT 2"
                }
            ];
            ConfigReloader.Reload(Settings);
            await Task.Delay(Settings.RefreshIntervalMs);
            SqlQueryManagerMock.Verify(a => a.ExecuteQuery("SELECT 2"));
        }
    }
}
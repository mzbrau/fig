using Moq;

namespace Fig.Integration.SqlLookupTableService.Test
{
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
    }
}
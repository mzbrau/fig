using Fig.Common.NetStandard.Scripting;
using Fig.Web.Dashboards.Runtime;
using Fig.Web.Facades;
using Fig.Web.Scripting;
using Moq;

namespace Fig.Unit.Test.Web.Dashboards;

internal static class DashboardTestHelpers
{
    public static DashboardTransformEngine CreateTransformEngine(bool allowDisplayScripts = true)
    {
        var configurationFacade = new Mock<IConfigurationFacade>();
        configurationFacade.SetupGet(x => x.AllowDisplayScripts).Returns(allowDisplayScripts);
        configurationFacade.SetupGet(x => x.WebFeaturesLoaded).Returns(true);
        return new DashboardTransformEngine(new JintEngineFactory(), configurationFacade.Object);
    }
}

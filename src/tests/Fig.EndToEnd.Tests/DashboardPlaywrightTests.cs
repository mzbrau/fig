using Fig.EndToEnd.Tests.Pages;
using Microsoft.Playwright;
using NUnit.Framework;

namespace Fig.EndToEnd.Tests;

[TestFixture]
public class DashboardPlaywrightTests : EndToEndTestBase
{
    [Test]
    [Ignore("Requires dashboard-capable E2E harness / wallboard user in CI")]
    public async Task AdminCanCreateAndOpenDashboard()
    {
        var page = await GetPage();
        var loginPage = new LoginPage(page);

        await loginPage.Login("admin", "admin");
        await page.GotoAsync("/dashboards");
        await page.GetByRole(AriaRole.Button, new() { Name = "New Dashboard" }).ClickAsync();
        await Expect(page.Locator(".dashboards-page")).ToBeVisibleAsync();
    }

    [Test]
    [Ignore("Requires dashboard-capable E2E harness / wallboard user in CI")]
    public async Task DashboardRoleSeesDashboardsOnlyChrome()
    {
        var page = await GetPage();
        var loginPage = new LoginPage(page);

        await loginPage.Login("wallboard", "this is a complex password!");
        await page.GotoAsync("/dashboards?wallboard=1");
        await Expect(page.Locator(".fig-main-nav")).ToBeHiddenAsync();
    }

    private static ILocatorAssertions Expect(ILocator locator) => Assertions.Expect(locator);
}

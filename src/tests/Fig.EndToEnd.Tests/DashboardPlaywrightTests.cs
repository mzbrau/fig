using Fig.EndToEnd.Tests.Pages;
using Microsoft.Playwright;
using NUnit.Framework;

namespace Fig.EndToEnd.Tests;

[TestFixture]
[Category("E2E")]
[Ignore("Requires dashboard-capable E2E harness / wallboard user in CI")]
public class DashboardPlaywrightTests : EndToEndTestBase
{
    [Test]
    public async Task AdminCanCreateAndOpenDashboard()
    {
        var page = await GetPage();
        var loginPage = new LoginPage(page);

        await loginPage.Login("admin", "admin");
        await page.GotoAsync("/dashboards");
        await page.GetByRole(AriaRole.Button, new() { Name = "New Dashboard" }).ClickAsync();
        await Assertions.Expect(page.Locator(".dashboards-page")).ToBeVisibleAsync();
    }

    [Test]
    public async Task DashboardRoleSeesDashboardsOnlyChrome()
    {
        var page = await GetPage();
        var loginPage = new LoginPage(page);

        await loginPage.Login("wallboard", "this is a complex password!");
        await page.GotoAsync("/dashboards?wallboard=1");
        await Assertions.Expect(page.Locator(".fig-main-nav")).ToBeHiddenAsync();
    }
}

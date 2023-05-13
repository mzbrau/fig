using Fig.EndToEnd.Tests.Pages;
using NUnit.Framework;

namespace Fig.EndToEnd.Tests;

[TestFixture]
public class PlaywrightTests : EndToEndTestBase
{
    //[Test]
    public async Task RunTest()
    {
        var page = await GetPage();
        var loginPage = new LoginPage(page);
        var settingsPage = new SettingsPage(page);

        await loginPage.Login("admin", "admin");

        await settingsPage.SelectClient("ClientA");

        await settingsPage.UpdateStringSetting("AnotherAddress", "Michael");
    }
}
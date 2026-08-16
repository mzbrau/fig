using Fig.EndToEnd.Tests.Pages;
using Microsoft.Playwright;
using NUnit.Framework;

namespace Fig.EndToEnd.Tests;

[TestFixture]
[Category("E2E")]
[NonParallelizable]
public class SettingsWorkflowTests : EndToEndTestBase
{
    [Test]
    public async Task Settings_Load_ShowsAspNetApiDefaults()
    {
        var page = await GetPage();
        var loginPage = new LoginPage(page);
        var settingsPage = new SettingsPage(page);

        await loginPage.Login("admin", "admin");
        await settingsPage.SelectClient("AspNetApi");
        await settingsPage.WaitForSetting("AppVersion");

        var appVersion = await settingsPage.GetStringSettingValue("AppVersion");
        Assert.That(appVersion, Is.Not.Null.And.Not.Empty);

        await settingsPage.WaitForSetting("ExternalApiUrl");
        var externalApiUrl = await settingsPage.GetStringSettingValue("ExternalApiUrl");
        Assert.That(externalApiUrl, Is.EqualTo("https://api.example.com"));
    }

    [Test]
    public async Task DisplayScript_JavaScriptValidation_ShowsAndClears()
    {
        var page = await GetPage();
        var loginPage = new LoginPage(page);
        var settingsPage = new SettingsPage(page);

        await loginPage.Login("admin", "admin");
        await settingsPage.SelectClient("DisplayScriptExample");
        await settingsPage.WaitForSetting("ServerIpAddress");

        await settingsPage.UpdateStringSetting("ServerIpAddress", "not-an-ip", expectSaveEnabled: false);
        await page.Locator("[data-test-id=\"ServerIpAddress\"]").ScrollIntoViewIfNeededAsync();
        await Assertions.Expect(settingsPage.ValidationAnnotation("ServerIpAddress"))
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });

        await settingsPage.UpdateStringSetting("ServerIpAddress", "192.168.1.1", expectSaveEnabled: false);
        await Assertions.Expect(settingsPage.ValidationAnnotation("ServerIpAddress"))
            .ToBeHiddenAsync(new LocatorAssertionsToBeHiddenOptions { Timeout = 30_000 });
    }

    [Test]
    public async Task Settings_UpdatePersists_AndHistoryShowsChange()
    {
        var page = await GetPage();
        var loginPage = new LoginPage(page);
        var settingsPage = new SettingsPage(page);
        var newValue = $"e2e-{Guid.NewGuid():N}".Substring(0, 20);

        await loginPage.Login("admin", "admin");
        await settingsPage.SelectClient("AspNetApi");
        await settingsPage.UpdateStringSetting("AppVersion", newValue);
        await settingsPage.SaveWithMessage("E2E update persist");

        await page.EvaluateAsync("() => { localStorage.clear(); sessionStorage.clear(); }");
        await page.GotoAsync("/", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 120_000
        });
        await loginPage.Login("admin", "admin");
        await settingsPage.SelectClient("AspNetApi");

        var persisted = await settingsPage.GetStringSettingValue("AppVersion");
        Assert.That(persisted, Is.EqualTo(newValue));

        await settingsPage.ClickHistory("AppVersion");
        await Assertions.Expect(settingsPage.HistoryGrid("AppVersion"))
            .ToContainTextAsync(newValue);
        await Assertions.Expect(settingsPage.HistoryGrid("AppVersion"))
            .ToContainTextAsync("admin");
    }

    [Test]
    public async Task Settings_Undo_RestoresUnsavedValue()
    {
        var page = await GetPage();
        var loginPage = new LoginPage(page);
        var settingsPage = new SettingsPage(page);

        await loginPage.Login("admin", "admin");
        await settingsPage.SelectClient("AspNetApi");

        var original = await settingsPage.GetStringSettingValue("Location");
        await settingsPage.UpdateStringSetting("Location", "temporary-e2e-value");
        Assert.That(await settingsPage.GetStringSettingValue("Location"), Is.EqualTo("temporary-e2e-value"));

        await settingsPage.ClickUndo("Location");

        var restored = await settingsPage.GetStringSettingValue("Location");
        Assert.That(restored, Is.EqualTo(original));
    }

    [Test]
    public async Task Settings_ResetToDefault_RestoresDefaultAfterSave()
    {
        var page = await GetPage();
        var loginPage = new LoginPage(page);
        var settingsPage = new SettingsPage(page);
        const string settingName = "EnvironmentName2";
        const string defaultValue = "Development";
        var changedValue = $"env-{Guid.NewGuid():N}"[..16];

        await loginPage.Login("admin", "admin");
        await settingsPage.SelectClient("AspNetApi");

        await settingsPage.UpdateStringSetting(settingName, changedValue);
        await settingsPage.SaveWithMessage("E2E change before reset");
        Assert.That(await settingsPage.GetStringSettingValue(settingName), Is.EqualTo(changedValue));

        await settingsPage.ClickReset(settingName);
        Assert.That(await settingsPage.GetStringSettingValue(settingName), Is.EqualTo(defaultValue));
        await settingsPage.SaveWithMessage("E2E reset to default");

        var value = await settingsPage.GetStringSettingValue(settingName);
        Assert.That(value, Is.EqualTo(defaultValue));
    }
}

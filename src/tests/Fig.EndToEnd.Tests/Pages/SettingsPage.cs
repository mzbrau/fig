using Microsoft.Playwright;

namespace Fig.EndToEnd.Tests.Pages;

public class SettingsPage : PageObjectModel
{
    public SettingsPage(IPage page) : base(page)
    {
    }

    public async Task SelectClient(string clientName)
    {
        var client = Page.Locator($"[data-test-id=\"{clientName}\"]");
        await client.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await client.ClickAsync();
        await Page.Locator("[data-test-id=\"SaveSettings\"]")
            .WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await ExpandAll();
    }

    public async Task ExpandAll()
    {
        var expandAll = Page.Locator("[data-test-id=\"ExpandAllSettings\"]");
        await expandAll.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await expandAll.ClickAsync();
    }

    public async Task WaitForSetting(string settingName)
    {
        await Page.Locator($"[data-test-id=\"{settingName}_card\"], [data-test-id=\"{settingName}\"]")
            .First
            .WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
    }

    public async Task ExpandSettingIfNeeded(string settingName)
    {
        var editor = Page.Locator($"[data-test-id=\"{settingName}\"]");
        if (await editor.CountAsync() > 0 && await editor.First.IsVisibleAsync())
            return;

        var expand = Page.Locator($"[data-test-id=\"{settingName}_expand\"]");
        if (await expand.CountAsync() > 0)
        {
            var card = Page.Locator($"[data-test-id=\"{settingName}_card\"]");
            await card.HoverAsync();
            await expand.ClickAsync(new LocatorClickOptions { Force = true });
        }

        await editor.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
    }

    public async Task UpdateStringSetting(string settingName, string value, bool expectSaveEnabled = true)
    {
        await ExpandSettingIfNeeded(settingName);
        var locator = Page.Locator($"[data-test-id=\"{settingName}\"]");
        await locator.FillAsync(value);
        await locator.PressAsync("Tab");
        if (expectSaveEnabled)
        {
            await Assertions.Expect(Page.Locator("[data-test-id=\"SaveSettings\"]"))
                .ToBeEnabledAsync(new LocatorAssertionsToBeEnabledOptions { Timeout = 15_000 });
        }
    }

    public async Task<string> GetStringSettingValue(string settingName)
    {
        await ExpandSettingIfNeeded(settingName);
        return await Page.Locator($"[data-test-id=\"{settingName}\"]").InputValueAsync();
    }

    public async Task ClickUndo(string settingName)
    {
        await Page.Locator($"[data-test-id=\"{settingName}_undo\"]").ClickAsync();
    }

    public async Task ClickReset(string settingName)
    {
        var reset = Page.Locator($"[data-test-id=\"{settingName}_reset\"]");
        await Assertions.Expect(reset).ToBeEnabledAsync(new LocatorAssertionsToBeEnabledOptions
        {
            Timeout = 15_000
        });
        await reset.ClickAsync();
        await Assertions.Expect(Page.Locator("[data-test-id=\"SaveSettings\"]"))
            .ToBeEnabledAsync(new LocatorAssertionsToBeEnabledOptions { Timeout = 15_000 });
    }

    public async Task ClickHistory(string settingName)
    {
        await Page.Locator($"[data-test-id=\"{settingName}_history\"]").ClickAsync();
        await Page.Locator($"[data-test-id=\"{settingName}_history_grid\"]")
            .WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
    }

    public ILocator ModifiedBadge(string settingName) =>
        Page.Locator($"[data-test-id=\"{settingName}_modified\"]");

    public ILocator ValidationAnnotation(string settingName) =>
        Page.Locator($"[data-test-id=\"{settingName}_validation\"]");

    public ILocator HistoryGrid(string settingName) =>
        Page.Locator($"[data-test-id=\"{settingName}_history_grid\"]");

    public async Task SaveWithMessage(string message)
    {
        var saveButton = Page.Locator("[data-test-id=\"SaveSettings\"]");
        await Assertions.Expect(saveButton).ToBeEnabledAsync(new LocatorAssertionsToBeEnabledOptions
        {
            Timeout = 30_000
        });
        await saveButton.ClickAsync();
        var messageBox = Page.Locator("[data-test-id=\"ChangeMessage\"]");
        await messageBox.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await messageBox.FillAsync(message);
        await Page.Locator("[data-test-id=\"ChangeMessageConfirm\"]").ClickAsync();
        await messageBox.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Hidden,
            Timeout = 60_000
        });
        await Assertions.Expect(saveButton).ToBeDisabledAsync(new LocatorAssertionsToBeDisabledOptions
        {
            Timeout = 30_000
        });
    }
}

using Microsoft.Playwright;

namespace Fig.EndToEnd.Tests.Pages;

public class SettingsPage : PageObjectModel
{
    public SettingsPage(IPage page) : base(page)
    {
    }

    public async Task SelectClient(string clientName)
    {
        await Page.Locator($"[data-test-id=\"{clientName}\"]").ClickAsync();
    }

    public async Task UpdateStringSetting(string settingName, string value)
    {
        var locator = Page.Locator($"[data-test-id=\"{settingName}\"]");
        await locator.FillAsync(value);
        await locator.PressAsync("Tab");
    }
}
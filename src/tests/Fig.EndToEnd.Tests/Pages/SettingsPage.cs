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
        
        var persistedValue = await locator.InputValueAsync();
        if (persistedValue != value)
        {
            throw new Exception($"Failed to persist the value for setting '{settingName}'. Expected: '{value}', but got: '{persistedValue}'");
        }
    }

    public async Task UpdateBoolSetting(string settingName, bool value)
    {
        var toggle = Page.Locator($"[data-test-id=\"{settingName}\"]");
        if ((await toggle.IsCheckedAsync()) != value)
        {
            await toggle.ClickAsync();
        }
    }

    public async Task UpdateDateTimeSetting(string settingName, DateTime value)
    {
        var picker = Page.Locator($"[data-test-id=\"{settingName}\"]");
        await picker.FillAsync(value.ToString("MM/dd/yyyy HH:mm"));
    }

    public async Task UpdateNumericSetting(string settingName, string value)
    {
        var input = Page.Locator($"[data-test-id=\"{settingName}\"]");
        await input.FillAsync(value);
        await input.PressAsync("Tab");
    }

    public async Task UpdateJsonSetting(string settingName, string json)
    {
        var textarea = Page.Locator($"[data-test-id=\"{settingName}\"]");
        await textarea.FillAsync(json);
        await Page.Locator($"[data-test-id=\"{settingName}-format-button\"]").ClickAsync();
    }

    public async Task<string> GetValidationMessage(string settingName)
    {
        var validation = Page.Locator($"[data-test-id=\"{settingName}-validation\"]");
        return await validation.TextContentAsync() ?? string.Empty;
    }

    public async Task UpdateSecretSetting(string settingName, string newValue)
    {
        await Page.Locator($"[data-test-id=\"{settingName}_edit\"]").ClickAsync();
        await Page.Locator($"[data-test-id=\"{settingName}_newpass\"]").FillAsync(newValue);
        await Page.Locator($"[data-test-id=\"{settingName}_confirm\"]").FillAsync(newValue);
        await Page.Locator($"[data-test-id=\"{settingName}_save\"]").ClickAsync();
    }
}
using Microsoft.Playwright;

namespace Fig.EndToEnd.Tests.Pages;

public class LoginPage : PageObjectModel
{
    private readonly ILocator _usernameField;
    private readonly ILocator _passwordField;
    private readonly ILocator _loginButton;

    public LoginPage(IPage page) : base(page)
    {
        _usernameField = Page.Locator("[data-test-id=\"Username\"]");
        _passwordField = Page.Locator("[data-test-id=\"Password\"]");
        _loginButton = Page.Locator("[data-test-id=\"LoginButton\"]");
    }

    public async Task Login(string username, string password)
    {
        await _usernameField.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await _usernameField.FillAsync(username);
        await _passwordField.FillAsync(password);
        await _loginButton.ClickAsync();
        await Page.Locator("[data-test-id=\"AspNetApi\"], [data-test-id=\"DisplayScriptExample\"]")
            .First
            .WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 120_000
            });
    }
}

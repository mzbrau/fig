using Microsoft.Playwright;

namespace Fig.EndToEnd.Tests.Pages;

public class LoginPage : PageObjectModel
{
    private readonly ILocator _usernameField;
    private readonly ILocator _passwordField;
    private readonly ILocator _loginButton;
    
    public LoginPage(IPage page) : base(page)
    {
        _usernameField = Page.Locator("input[name=\"Username\"]");
        _passwordField = Page.Locator("input[name=\"Password\"]");
        _loginButton = Page.Locator("[data-test-id=\"LoginButton\"]");
    }

    public async Task Login(string username, string password)
    {
        await _usernameField.FillAsync(username);
        await _passwordField.FillAsync(password);
        await _loginButton.ClickAsync();
    }
}
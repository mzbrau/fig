using Microsoft.Playwright;
using NUnit.Framework;

namespace Fig.EndToEnd.Tests;

public abstract class EndToEndTestBase
{
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    
    [OneTimeSetUp]
    public async Task FixtureSetup()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
        });
    }
    
    [OneTimeTearDown]
    public async Task FixtureTearDown()
    {
        _playwright.Dispose();
        await _browser.DisposeAsync();
    }
    
    protected async Task<IPage> GetPage()
    {
        var options = new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true
        };
        
        var context = await _browser.NewContextAsync(options);

        // Open new page
        var page = await context.NewPageAsync();
        await page.GotoAsync("https://localhost:7148/");

        return page;
    }
}
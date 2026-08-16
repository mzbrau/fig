using Microsoft.Playwright;
using NUnit.Framework;

namespace Fig.EndToEnd.Tests;

public abstract class EndToEndTestBase
{
    private IBrowserContext? _context;

    protected string WebBaseUrl =>
        Environment.GetEnvironmentVariable("FIG_E2E_WEB_URL")?.TrimEnd('/')
        ?? AspireFixture.WebBaseUrl;

    [TearDown]
    public async Task TearDownContext()
    {
        if (_context is not null)
            await _context.DisposeAsync();
    }

    protected async Task<IPage> GetPage()
    {
        _context = await AspireFixture.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
            BaseURL = WebBaseUrl,
            ViewportSize = new ViewportSize { Width = 1440, Height = 900 }
        });

        _context.SetDefaultTimeout(60_000);
        var page = await _context.NewPageAsync();
        await page.GotoAsync("/", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 120_000
        });

        return page;
    }
}

using Microsoft.Playwright;

namespace Fig.EndToEnd.Tests.Pages;

public abstract class PageObjectModel
{
    protected readonly IPage Page;

    protected PageObjectModel(IPage page)
    {
        Page = page;
    }
}
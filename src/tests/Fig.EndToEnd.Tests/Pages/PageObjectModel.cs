using Microsoft.Playwright;

namespace Fig.EndToEnd.Tests.Pages;

public abstract class PageObjectModel
{
    protected readonly IPage Page;

    protected PageObjectModel(IPage page)
    {
        Page = page;
    }

    protected async Task DismissPostLoginDialogsAsync(TimeSpan? pollDuration = null)
    {
        var duration = pollDuration ?? TimeSpan.FromSeconds(3);
        var deadline = DateTime.UtcNow + duration;

        while (DateTime.UtcNow < deadline)
        {
            if (await TryDismissVisibleDialogAsync())
            {
                // Another dialog (e.g. What's New after JS dialog) may open next.
                deadline = DateTime.UtcNow + TimeSpan.FromSeconds(1);
                await Task.Delay(250);
                continue;
            }

            await Task.Delay(250);
        }

        await TryDismissVisibleDialogAsync();
    }

    private async Task<bool> TryDismissVisibleDialogAsync()
    {
        var mask = Page.Locator(".rz-dialog-mask");
        if (await mask.CountAsync() == 0 || !await mask.First.IsVisibleAsync())
            return false;

        foreach (var name in new[] { "Dismiss", "Close", "Done" })
        {
            var button = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = name });
            if (await button.CountAsync() == 0)
                continue;

            await button.First.ClickAsync();
            await mask.First.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Hidden,
                Timeout = 10_000
            });
            return true;
        }

        return false;
    }
}

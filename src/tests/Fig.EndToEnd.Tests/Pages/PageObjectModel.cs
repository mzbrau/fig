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
        var mask = Page.Locator(".rz-dialog-mask").First;
        if (await Page.Locator(".rz-dialog-mask").CountAsync() == 0 || !await mask.IsVisibleAsync())
            return false;

        await Page.Keyboard.PressAsync("Escape");
        if (await WaitForMaskHiddenAsync(mask))
            return true;

        var dialog = Page.Locator(".rz-dialog").Last;
        foreach (var name in new[] { "Dismiss", "Close", "Done" })
        {
            var button = dialog.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions
            {
                Name = name,
                Exact = true
            });
            if (await button.CountAsync() == 0)
                continue;

            // Prefer Last so footer "Close" wins over the header icon-only close control.
            await button.Last.ClickAsync();
            if (await WaitForMaskHiddenAsync(mask))
                return true;
        }

        return false;
    }

    private static async Task<bool> WaitForMaskHiddenAsync(ILocator mask)
    {
        try
        {
            await mask.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Hidden,
                Timeout = 3_000
            });
            return true;
        }
        catch (Exception ex) when (ex is TimeoutException or System.TimeoutException)
        {
            return false;
        }
    }
}

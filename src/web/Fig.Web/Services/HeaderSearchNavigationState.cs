namespace Fig.Web.Services;

public sealed class HeaderSearchNavigationState
{
    private bool _openSearchOnNextSettingsRender;

    public void RequestOpenSearch()
    {
        _openSearchOnNextSettingsRender = true;
    }

    public bool ConsumeOpenSearchRequest()
    {
        if (!_openSearchOnNextSettingsRender)
        {
            return false;
        }

        _openSearchOnNextSettingsRender = false;
        return true;
    }
}

using Radzen;

namespace Fig.Web.ReleaseHighlights;

public static class ReleaseHighlightsDialogOptionsFactory
{
    public static DialogOptions Create()
    {
        return new DialogOptions
        {
            Width = "820px",
            Resizable = true,
            Draggable = true,
            CloseDialogOnOverlayClick = true,
            CloseDialogOnEsc = true,
            ShowTitle = false,
            ShowClose = false,
            Style = "background: #1e1e1e; max-width: 92vw;"
        };
    }
}

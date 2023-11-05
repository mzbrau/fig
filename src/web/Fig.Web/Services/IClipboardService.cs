namespace Fig.Web.Services;

public interface IClipboardService
{
    ValueTask WriteTextAsync(string text);
}
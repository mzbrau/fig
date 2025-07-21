namespace Fig.Contracts.ImportExport;

public class HeadingExportDataContract
{
    public HeadingExportDataContract(string text, string? color, bool advanced)
    {
        Text = text;
        Color = color;
        Advanced = advanced;
    }
    
    /// <summary>
    /// The text to display in the heading.
    /// </summary>
    public string Text { get; }
    
    /// <summary>
    /// The color for the heading border. May be null if it should inherit from the setting.
    /// </summary>
    public string? Color { get; }
    
    /// <summary>
    /// Whether the heading should be hidden when advanced settings are hidden.
    /// </summary>
    public bool Advanced { get; }
}

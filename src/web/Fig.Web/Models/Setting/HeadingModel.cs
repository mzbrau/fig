using Fig.Contracts.SettingDefinitions;

namespace Fig.Web.Models.Setting;

public class HeadingModel
{
    public HeadingModel(HeadingDataContract headingDataContract)
    {
        Text = headingDataContract.Text;
        Color = headingDataContract.Color;
        Advanced = headingDataContract.Advanced;
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

    public static HeadingModel CreateForCustomAction()
    {
        return new HeadingModel(new HeadingDataContract("Custom Actions", "#FF0000", false));
    }
}

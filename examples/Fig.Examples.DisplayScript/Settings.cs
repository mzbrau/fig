using Fig.Client;
using Fig.Client.Attributes;
using Fig.Client.Enums;
using Microsoft.Extensions.Logging;

namespace Fig.Examples.DisplayScript;

public class Settings : SettingsBase
{
    public override string ClientDescription => "A collection of examples on the use of Display Scripts";

    [Setting("The mode that this application will run in.")]
    [ValidValues("Mode A", "Mode B")]
    [Category("Mode Example", CategoryColor.Blue)]
    [DisplayScript(Scripts.SelectMode)]
    [DisplayOrder(1)]
    public string Mode { get; set; } = "Mode A";

    [Setting("Example setting in mode A.")]
    [Category("Mode Example", CategoryColor.Blue)]
    [DisplayOrder(2)]
    public string ModeASetting { get; set; } = "Some Value";

    [Setting("Example setting in mode B.")]
    [Category("Mode Example", CategoryColor.Blue)]
    [DisplayOrder(3)]
    public string ModeBSetting1 { get; set; } = "Thing";

    [Setting("Another example setting in mode B.")]
    [Category("Mode Example", CategoryColor.Blue)]
    [DisplayOrder(4)]
    public string ModeBSetting2 { get; set; } = "Another thing";
    
    [Setting("True if security should be used")]
    [Category("Multi Setting Validation Example", CategoryColor.Green)]
    [DisplayOrder(5)]
    [DisplayScript(Scripts.ValidateSecurity)]
    public bool UseSecurity1 { get; set; }

    [Setting("The URL to connect to")]
    [Category("Multi Setting Validation Example", CategoryColor.Green)]
    [DisplayOrder(6)]
    [DisplayScript(Scripts.ValidateSecurity)]
    public string Url1 { get; set; } = "http://www.google.com";
    
    [Setting("True if security should be used")]
    [Category("Multi Setting Value Update Example", CategoryColor.Red)]
    [DisplayOrder(7)]
    [DisplayScript(Scripts.AutoUpdateValue)]
    public bool UseSecurity2 { get; set; }

    [Setting("The URL to connect to")]
    [Category("Multi Setting Value Update Example", CategoryColor.Red)]
    [DisplayOrder(8)]
    [DisplayScript(Scripts.AutoUpdateValue)]
    public string Url2 { get; set; } = "http://www.google.com";
    
    [Setting("A set of groups that can be referenced")]
    [Category("Dynamic Valid Values Example", CategoryColor.Orange)]
    [DisplayOrder(9)]
    [DisplayScript(Scripts.UpdateValidValues)]
    public List<string>? Groups { get; set; }
    
    [Setting("A set of services that can be grouped", defaultValueMethodName:nameof(GetDefaultServices))]
    [Category("Dynamic Valid Values Example", CategoryColor.Orange)]
    [DisplayOrder(10)]
    [DisplayScript(Scripts.UpdateValidValues)]
    public List<Service>? Services { get; set; }

    [Setting("A set of options to manipulate settings")]
    [Category("Setting Manipulation", CategoryColor.Purple)]
    [DisplayOrder(11)]
    [ValidValues("Select Option...")]
    [DisplayScript(Scripts.ControlOtherSettings)]
    public string Option { get; set; } = "Select Option...";
    
    [Setting("A setting to be manipulated")]
    [Category("Setting Manipulation", CategoryColor.Purple)]
    [DisplayOrder(12)]
    [MultiLine(2)]
    public string? ControlledString { get; set; }
    
    [Setting("A setting to be manipulated")]
    [Category("Setting Manipulation", CategoryColor.Purple)]
    [DisplayOrder(13)]
    public int ControlledInt { get; set; }
    
    [Setting("A setting to be manipulated")]
    [Category("Setting Manipulation", CategoryColor.Purple)]
    [DisplayOrder(14)]
    public bool ControlledBool { get; set; }
    
    [Setting("A setting to be manipulated")]
    [Category("Setting Manipulation", CategoryColor.Purple)]
    [DisplayOrder(15)]
    public long ControlledLong { get; set; }
    
    [Setting("A setting to be manipulated")]
    [Category("Setting Manipulation", CategoryColor.Purple)]
    [DisplayOrder(16)]
    public double ControlledDouble { get; set; }
    
    [Setting("A setting to be manipulated")]
    [Category("Setting Manipulation", CategoryColor.Purple)]
    [DisplayOrder(17)]
    public DateTime? ControlledDateTime { get; set; }
    
    [Setting("A setting to be manipulated")]
    [Category("Setting Manipulation", CategoryColor.Purple)]
    [DisplayOrder(18)]
    public TimeSpan? ControlledTimeSpan { get; set; }
    
    public override void Validate(ILogger logger)
    {
        // Validation logic
    }

    public static List<Service> GetDefaultServices()
    {
        List<Service> result = new();
        for (int i = 0; i < 1; i++)
        {
            result.Add(new Service()
            {
                Name = $"Service {i}"
            });
        }

        return result;
    }
}
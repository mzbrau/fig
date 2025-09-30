using Fig.Client;
using Fig.Client.Abstractions.Attributes;
using Fig.Client.Abstractions.Enums;

namespace Fig.Examples.DisplayScript;

public class Settings : SettingsBase
{
    public override string ClientDescription => "A collection of examples on the use of Display Scripts";

    [Setting("The mode that this application will run in.")]
    [ValidValues("Mode A", "Mode B")]
    [Category("Mode Example", CategoryColor.Blue)]
    [DisplayScript(Scripts.SelectMode)]
    public string Mode { get; set; } = "Mode A";

    [Setting("Example setting in mode A.")]
    [Category("Mode Example", CategoryColor.Blue)]
    public string ModeASetting { get; set; } = "Some Value";

    [Setting("Example setting in mode B.")]
    [Category("Mode Example", CategoryColor.Blue)]
    public string ModeBSetting1 { get; set; } = "Thing";

    [Setting("Another example setting in mode B.")]
    [Category("Mode Example", CategoryColor.Blue)]
    public string ModeBSetting2 { get; set; } = "Another thing";
    
    [Setting("True if security should be used")]
    [Category("Multi Setting Validation Example", CategoryColor.Green)]
    [DisplayScript(Scripts.ValidateSecurity)]
    public bool UseSecurity1 { get; set; }

    [Setting("The URL to connect to")]
    [Category("Multi Setting Validation Example", CategoryColor.Green)]
    [DisplayScript(Scripts.ValidateSecurity)]
    public string Url1 { get; set; } = "http://www.google.com";
    
    [Setting("True if security should be used")]
    [Category("Multi Setting Value Update Example", CategoryColor.Red)]
    [DisplayScript(Scripts.AutoUpdateValue)]
    public bool UseSecurity2 { get; set; }

    [Setting("The URL to connect to")]
    [Category("Multi Setting Value Update Example", CategoryColor.Red)]
    [DisplayScript(Scripts.AutoUpdateValue)]
    public string Url2 { get; set; } = "http://www.google.com";
    
    [Setting("A set of groups that can be referenced")]
    [Category("Dynamic Valid Values Example", CategoryColor.Orange)]
    [DisplayScript(Scripts.UpdateValidValues)]
    public List<string>? Groups { get; set; }
    
    [Setting("A set of services that can be grouped", defaultValueMethodName:nameof(GetDefaultServices))]
    [Category("Dynamic Valid Values Example", CategoryColor.Orange)]
    [DisplayScript(Scripts.UpdateValidValues)]
    public List<Service>? Services { get; set; }

    [Setting("A set of options to manipulate settings")]
    [Category("Setting Manipulation", CategoryColor.Purple)]
    [ValidValues("Select Option...")]
    [DisplayScript(Scripts.ControlOtherSettings)]
    public string Option { get; set; } = "Select Option...";
    
    [Setting("A setting to be manipulated")]
    [Category("Setting Manipulation", CategoryColor.Purple)]
    [MultiLine(2)]
    public string? ControlledString { get; set; }
    
    [Setting("A setting to be manipulated")]
    [Category("Setting Manipulation", CategoryColor.Purple)]
    public int ControlledInt { get; set; }
    
    [Setting("A setting to be manipulated")]
    [Category("Setting Manipulation", CategoryColor.Purple)]
    public bool ControlledBool { get; set; }
    
    [Setting("A setting to be manipulated")]
    [Category("Setting Manipulation", CategoryColor.Purple)]
    public long ControlledLong { get; set; }
    
    [Setting("A setting to be manipulated")]
    [Category("Setting Manipulation", CategoryColor.Purple)]
    public double ControlledDouble { get; set; }
    
    [Setting("A setting to be manipulated")]
    [Category("Setting Manipulation", CategoryColor.Purple)]
    public DateTime? ControlledDateTime { get; set; }
    
    [Setting("A setting to be manipulated")]
    [Category("Setting Manipulation", CategoryColor.Purple)]
    public TimeSpan? ControlledTimeSpan { get; set; }
    
    [NestedSetting]
    [Category("Nested Settings Example", CategoryColor.Green)]
    public DatabaseConnection Connection { get; set; } = new();
    
    public override IEnumerable<string> GetValidationErrors()
    {
        return [];
    }

    public static List<Service> GetDefaultServices()
    {
        List<Service> result = new();
        for (int i = 0; i < 1; i++)
        {
            result.Add(new Service()
            {
                Name = $"Service {i}",
                Group = "Placeholder",
                ValidationType = "200OK",
                CustomString = "Default value"
            });
        }

        return result;
    }
}

public class DatabaseConnection
{
    [Setting("Database server hostname")]
    [Category("Nested Settings Example", CategoryColor.Green)]
    [DisplayScript(@"
        // Before the fix: You would need to reference nested settings using full paths like 'Connection->Host'
        // After the fix: You can reference nested settings using dot notation like 'Connection.Host'
        if (Connection.Host.Value === 'localhost') {
            Connection.Host.ValidationExplanation = 'Using localhost - consider using a dedicated database server for production';
            Connection.Port.Value = 5432; // Default PostgreSQL port for localhost
        } else if (Connection.Host.Value === 'production-db') {
            Connection.Port.Value = 5433; // Custom port for production
            Connection.Auth.Username.Value = 'prod_user';
        } else {
            Connection.Host.ValidationExplanation = '';
            Connection.Port.Value = 5432;
            Connection.Auth.Username.Value = 'default_user';
        }
    ")]
    public string Host { get; set; } = "localhost";
    
    [Setting("Database server port")]
    [Category("Nested Settings Example", CategoryColor.Green)]
    public int Port { get; set; } = 5432;
    
    [NestedSetting]
    [Category("Nested Settings Example", CategoryColor.Green)]
    public DatabaseAuth Auth { get; set; } = new();
}

public class DatabaseAuth  
{
    [Setting("Database username")]
    [Category("Nested Settings Example", CategoryColor.Green)]
    [DisplayScript(@"
        // This script can reference nested settings using dot notation
        // instead of needing 'Connection->Auth->Username' and 'Connection->Auth->Password'
        if (Connection.Auth.Username.Value === 'admin') {
            Connection.Auth.Username.ValidationExplanation = 'Admin user detected - ensure strong password is used';
            Connection.Auth.Password.IsReadOnly = false;
        } else {
            Connection.Auth.Password.IsReadOnly = true;
            Connection.Auth.Username.ValidationExplanation = '';
        }
    ")]
    public string Username { get; set; } = "default_user";
    
    [Setting("Database password")]
    [Category("Nested Settings Example", CategoryColor.Green)]
    [Secret]
    public string Password { get; set; } = "defaultpassword";
}
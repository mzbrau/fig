using Fig.Client;
using Fig.Client.Attributes;
using Fig.Client.Enums;
using Fig.Client.Validation;
using Microsoft.Extensions.Logging;

namespace Fig.Examples.ConsoleApp;

public class ConsoleSettings : SettingsBase
{
    public override string ClientDescription => "$Fig.Examples.ConsoleApp.ConsoleApp.md,$Fig.Examples.ConsoleApp.ConsoleApp2.md";

    [Setting("$Fig.Examples.ConsoleApp.ConsoleApp.md#UseService,$Fig.Examples.ConsoleApp.ConsoleApp2.md#OtherFile",
        false)]
    //[EnablesSettings(nameof(ServiceUsername), nameof(ServicePassword))]
    [DisplayOrder(1)]
    [Category("Authentication", CategoryColor.Red)]
    [DisplayScript(@"if (UseService.Value == true) { ServiceUsername.Visible = true; ServicePassword.Visible = false; } else {
    ServicePassword.Visible = true;
    ServiceUsername.Visible = false;
}")]
    public bool UseService { get; set; } = false;
    
    [Setting("the username")]
    [DisplayOrder(2)]
    [Validation(ValidationType.NotEmpty)]
    [Category("Authentication", CategoryColor.Red)]
    public string? ServiceUsername { get; set; }
    
    [Setting("the password")]
    [Secret]
    [DisplayOrder(3)]
    [Category("Authentication", CategoryColor.Red)]
    public string? ServicePassword { get; set; }

    [Setting("some other setting")]
    [Category("Other", CategoryColor.Blue)]
    public int UnrelatedSetting { get; set; } = 1;

    [Setting("My Animals")]
    [Category("Things", CategoryColor.Orange)]
    public List<Animal>? MyAnimals { get; set; }

    public static List<Animal> GetAnimals()
    {
        return new List<Animal>()
        {
            new Animal
            {
                Name = "l",
                Legs = 4,
                FavouriteFood = "m"
            }
        };
    }

    public override void Validate(ILogger logger)
    {
        //Perform validation here.
        SetConfigurationErrorStatus(false);
    }

    public class Animal
    {
        [Validation("[0-9a-zA-Z]{5,}", "Must have 5 or more characters and a much longer thing that will test if this thing wraps or if it just goes out of scope. We will see I guess.")]
        public string Name { get; set; }
        
        public int Legs { get; set; }
        
        [Validation(ValidationType.NotEmpty)]
        public string FavouriteFood { get; set; }
        
        [ValidValues("one", "two", "three")]
        public List<string> Things { get; set; }
    }
}
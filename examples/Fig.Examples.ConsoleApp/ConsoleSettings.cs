using Fig.Client;
using Fig.Client.Attributes;
using Fig.Client.Enums;
using Fig.Client.Validation;
using Microsoft.Extensions.Logging;

namespace Fig.Examples.ConsoleApp;

public class ConsoleSettings : SettingsBase
{
    public override string ClientDescription => "$ConsoleApp,$ConsoleApp2";

    [Setting("$ConsoleApp#UseService",
        false)]
    //[EnablesSettings(nameof(ServiceUsername), nameof(ServicePassword))]
    [Category("Authentication", CategoryColor.Red)]
    [DisplayScript(@"if (UseService.Value == true) { ServiceUsername.Visible = true; ServicePassword.Visible = false; } else {
    ServicePassword.Visible = true;
    ServiceUsername.Visible = false;
}")]
    public bool UseService { get; set; } = false;
    
    [Setting("the username")]
    [Validation(ValidationType.NotEmpty)]
    [Category("Authentication", CategoryColor.Red)]
    public string? ServiceUsername { get; set; }
    
    [Setting("the password")]
    [Secret]
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

    public override IEnumerable<string> GetValidationErrors()
    {
        return [];
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
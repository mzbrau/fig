using Fig.Client.Attributes;
using Microsoft.Extensions.Logging;

namespace Fig.Test.Common.TestSettings;

public class EnumSettings : TestSettingsBase
{
    public override string ClientDescription => "Enum Settings";

    public override string ClientName => "EnumSettings";
    
    [Setting("Pet")]
    [ValidValues(typeof(Pets))]
    public Pets Pet { get; set; }

    [Setting("Pet")]
    [ValidValues(typeof(Pets))]
    public Pets PetWithDefault { get; set; } = Pets.Fish;
    
    [Setting("Optional Pet")]
    [ValidValues(typeof(Pets))]
    public Pets? OptionalPet { get; set; }

    [Setting("Optional Pet")]
    [ValidValues(typeof(Pets))]
    public Pets? OptionalPetWithDefault { get; set; } = Pets.Cat;
    
    [Setting("Pet Group")]
    public List<PetGroup> PetGroups { get; set; }
    
    [Setting("Pet Group", true, nameof (GetDefaultPetGroups))]
    public List<PetGroup> PetGroupsWithDefault { get; set; }
    
    public override IEnumerable<string> GetValidationErrors()
    {
        return [];
    }

    public static List<PetGroup> GetDefaultPetGroups()
    {
        return
        [
            new PetGroup
            {
                Name = "Group 1",
                PetInGroup = Pets.Dog,
                OptionalPetInGroup = Pets.Fish
            }
        ];
    }
}

public class PetGroup
{
    public string Name { get; set; }
    
    public Pets PetInGroup { get; set; }
    
    public Pets? OptionalPetInGroup { get; set; }
}
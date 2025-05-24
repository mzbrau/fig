using Fig.Client.Attributes;
using Microsoft.Extensions.Logging;

namespace Fig.Test.Common.TestSettings;

public class ClientBList : TestSettingsBase
{
    public override string ClientName => "ClientB";
    public override string ClientDescription => "ClientB";
    
    [Setting("Animals", defaultValueMethodName:nameof(GetAnimals))]
    public List<Animal> Animals { get; set; }
    
    public override IEnumerable<string> GetValidationErrors()
    {
        return [];
    }
    
    public static List<Animal> GetAnimals()
    {
        var animals = new List<Animal>();
        for (var i = 0; i < 2; i++)
        {
            animals.Add(new Animal()
            {
                Name = "Name" + i,
                Legs = i
            });
        }

        return animals;
    }
    
    public class Animal
    {
        public string Name { get; set; }
        
        public int Legs { get; set; }
    }
}
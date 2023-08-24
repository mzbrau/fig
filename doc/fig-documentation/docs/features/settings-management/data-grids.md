---
sidebar_position: 10

---

# Data Grids

Fig supports data grids for displaying complex settings.

## Usage

The following setting will result in a data grid with 3 columns, one for each property within the class. Items can be added, removed or edited as required.

```csharp
[Setting("Favourite Animals")]
public List<Animal> Animals { get; set; }

public class Animal
{
    public string Name { get; set; }

    public int Legs { get; set; }

    public string FavouriteFood { get; set; }
}
```

You can also create a data grid from a list of base types, for example:

```csharp
[Setting("Favourite Names")]
public List<string> Names { get; set; }
```

### Locking Data Grids

Data grids can also be locked. This prevents rows being added or removed. Existing rows can still be edited. To lock a data grid, used the **DataGridLocked** attribute.

```csharp
[Setting("Favourite Animals")]
[DataGridLocked]
public List<Animal> Animals { get; set; }
```

### Internal Attributes

Some attributes can also be used on the internal class including [MultiLine](https://www.figsettings.com/docs/features/settings-management/multiline) and [ValidValues](https://www.figsettings.com/docs/features/settings-management/valid-values). These work in the same way that they do on regular properties. In addition, there is a ReadOnly attributes which makes that column read only when editing the data grid.

```csharp
[Setting("Favourite Animals")]
public List<Animal> Animals { get; set; }

public class Animal
{
    [ReadOnly]
    public string Name { get; set; }

    [ValidValues("1", "2", "3")]
    public int Legs { get; set; }

	[MultiLine(3)]
    public string FavouriteFood { get; set; }
}
```

### Default Values

Data grids support default values but as they are complex objects, they cannot be specified within the attribute. To specify a default value, create a static class and reference it within the setting attribute. For example:

```csharp
[Setting("Favourite Animals", defaultValueMethodName: "GetAnimals")]
public List<Animal> Animals { get; set; }

public static List<Animal> GetAnimals()
{
    return new List<Animal>()
    {
        new Animal
        {
            Name = "Fluffy",
            Legs = 2,
            FavouriteFood = "carrots"
        },
        new Animal()
        {
            Name = "Rover",
            Legs = 4,
            FavouriteFood = "steak"
        }
    };
}
```

## Appearance

![image-20230824212148560](C:\Development\SideProjects\fig\doc\fig-documentation\static\img\image-20230824212148560.png)
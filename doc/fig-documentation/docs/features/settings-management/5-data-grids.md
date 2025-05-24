---
sidebar_position: 5
---

# Data Grids

Fig supports data grids for displaying complex settings.

## Usage

The following setting will result in a data grid with 3 columns, one for each property within the class. Items can be added, removed or edited as required.

For properties to be added they must:

- Be public
- Have a setter and a getter
- Not be ignored

```csharp
[Setting("Favorite Animals")]
public List<Animal> Animals { get; set; }

public class Animal
{
    public string Name { get; set; }

    public int Legs { get; set; }

    public string FavoriteFood { get; set; }
}
```

You can also create a data grid from a list of base types, for example:

```csharp
[Setting("Favorite Names")]
public List<string> Names { get; set; }
```

### Locking Data Grids

Data grids can also be locked. This prevents rows being added or removed. Existing rows can still be edited. To lock a data grid, used the **DataGridLocked** attribute.

```csharp
[Setting("Favorite Animals")]
[DataGridLocked]
public List<Animal> Animals { get; set; }
```

### Internal Attributes

Some attributes can also be used on the internal class including [MultiLine](https://www.figsettings.com/docs/features/settings-management/multiline),  [ValidValues](https://www.figsettings.com/docs/features/settings-management/valid-values), [Secret](https://www.figsettings.com/docs/features/settings-management/secret-settings) and [Validation](http://www.figsettings.com/docs/features/settings-management/validation). These work in the same way that they do on regular properties. In addition, there is:

- `[ReadOnly]` attribute which makes that column read only when editing the data grid.
- `[FigIgnore]` attribute which does not add that property into fig. It will not be shown in the UI or set by Fig in any way.

```csharp
[Setting("Favorite Animals")]
public List<Animal> Animals { get; set; }

public class Animal
{
    [ReadOnly]
    public string Name { get; set; }

    [ValidValues("1", "2", "3")]
    public int Legs { get; set; }

    [MultiLine(3)]
    [Validation(ValidationType.NotEmpty)]
    public string FavoriteFood { get; set; }

    [Secret]
    public string Password { get;set; }

    [FigIgnore]
    public string? MyOtherProperty { get; set; }

    // Note valid values must be set for List<string> within a data grid. 
    // Only List<string> is supported, not other enumerable types.
    [ValidValues("A", "B", "C")] 
    public List<string> Items { get; set; }
}
```

### Default Values

Data grids support default values but as they are complex objects, they cannot be specified within the attribute. To specify a default value, create a static class and reference it within the setting attribute. For example:

```csharp
[Setting("Favorite Animals", defaultValueMethodName: nameof(GetAnimals))]
public List<Animal> Animals { get; set; }

public static List<Animal> GetAnimals()
{
    return new List<Animal>()
    {
        new Animal
        {
            Name = "Fluffy",
            Legs = 2,
            FavoriteFood = "carrots"
        },
        new Animal()
        {
            Name = "Rover",
            Legs = 4,
            FavoriteFood = "steak"
        }
    };
}
```

## Appearance

![image-20230824212148560](../../../static/img/image-20230824212148560.png)

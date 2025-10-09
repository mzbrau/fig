namespace Fig.Web.Models.Setting;

public class CategoryFilterModel
{
    public CategoryFilterModel(string name, string color)
    {
        Name = name;
        Color = color;
    }
    
    public string Name { get; }
    public string Color { get; }

    public override bool Equals(object? obj)
    {
        if (obj is CategoryFilterModel other)
        {
            return Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return Name.ToLowerInvariant().GetHashCode();
    }
}

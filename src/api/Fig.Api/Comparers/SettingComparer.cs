using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Comparers;

public class SettingComparer : IEqualityComparer<SettingBusinessEntity>
{
    public bool Equals(SettingBusinessEntity x, SettingBusinessEntity y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (ReferenceEquals(x, null)) return false;
        if (ReferenceEquals(y, null)) return false;
        if (x.GetType() != y.GetType()) return false;
        return x.Name == y.Name && 
               x.Description == y.Description && 
               x.IsSecret == y.IsSecret &&
               Equals(x.DefaultValue, y.DefaultValue) &&
               x.ValidationType == y.ValidationType && 
               x.ValidationRegex == y.ValidationRegex &&
               x.ValidationExplanation == y.ValidationExplanation && 
               Equals(x.ValidValues, y.ValidValues) &&
               x.Group == y.Group && 
               x.DisplayOrder == y.DisplayOrder && 
               x.Advanced == y.Advanced &&
               x.StringFormat == y.StringFormat;
    }

    public int GetHashCode(SettingBusinessEntity obj)
    {
        var hashCode = new HashCode();
        hashCode.Add(obj.Name);
        hashCode.Add(obj.Description);
        hashCode.Add(obj.IsSecret);
        hashCode.Add(obj.DefaultValueAsJson);
        hashCode.Add(obj.ValidationType);
        hashCode.Add(obj.ValidationRegex);
        hashCode.Add(obj.ValidationExplanation);
        hashCode.Add(obj.ValidValues);
        hashCode.Add(obj.Group);
        hashCode.Add(obj.DisplayOrder);
        hashCode.Add(obj.Advanced);
        hashCode.Add(obj.StringFormat);
        return hashCode.ToHashCode();
    }
}
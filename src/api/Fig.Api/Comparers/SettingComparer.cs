using Fig.Datalayer.BusinessEntities;
using Newtonsoft.Json;

namespace Fig.Api.Comparers;

public class SettingComparer : IEqualityComparer<SettingBusinessEntity>
{
    public bool Equals(SettingBusinessEntity? x, SettingBusinessEntity? y)
    {
        if (ReferenceEquals(x, y))
            return true;
        if (ReferenceEquals(x, null))
            return false;
        if (ReferenceEquals(y, null))
            return false;
        if (x.GetType() != y.GetType())
            return false;

        return x.Name == y.Name &&
               x.Description == y.Description &&
               x.IsSecret == y.IsSecret &&
               JsonConvert.SerializeObject(x.DefaultValue) ==
               JsonConvert.SerializeObject(y.DefaultValue) &&
               x.ValidationType == y.ValidationType &&
               x.ValidationRegex == y.ValidationRegex &&
               x.ValidationExplanation == y.ValidationExplanation &&
               x.ValidValuesAsJson == y.ValidValuesAsJson &&
               x.Group == y.Group &&
               x.DisplayOrder == y.DisplayOrder &&
               x.Advanced == y.Advanced &&
               x.CommonEnumerationKey == y.CommonEnumerationKey &&
               x.JsonSchema == y.JsonSchema &&
               x.EditorLineCount == y.EditorLineCount &&
               x.DataGridDefinitionJson == y.DataGridDefinitionJson;
    }

    public int GetHashCode(SettingBusinessEntity obj)
    {
        var hashCode = new HashCode();
        hashCode.Add(obj.Name);
        hashCode.Add(obj.Description);
        hashCode.Add(obj.IsSecret);
        hashCode.Add(JsonConvert.SerializeObject(obj.DefaultValue));
        hashCode.Add(obj.ValidationType);
        hashCode.Add(obj.ValidationRegex);
        hashCode.Add(obj.ValidationExplanation);
        hashCode.Add(obj.ValidValuesAsJson);
        hashCode.Add(obj.Group);
        hashCode.Add(obj.DisplayOrder);
        hashCode.Add(obj.Advanced);
        hashCode.Add(obj.CommonEnumerationKey);
        hashCode.Add(obj.JsonSchema);
        hashCode.Add(obj.EditorLineCount);
        hashCode.Add(obj.DataGridDefinitionJson);
        return hashCode.ToHashCode();
    }
}
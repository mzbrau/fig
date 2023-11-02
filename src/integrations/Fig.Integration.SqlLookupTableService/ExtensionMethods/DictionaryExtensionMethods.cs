namespace Fig.Integration.SqlLookupTableService.ExtensionMethods;

public static class DictionaryExtensionMethods
{
    public static bool ContentEquals<TKey, TValue>(this Dictionary<TKey, TValue> dictionary,
        Dictionary<TKey, TValue> otherDictionary) where TKey : notnull
    {
        return (otherDictionary)
            .OrderBy(kvp => kvp.Key)
            .SequenceEqual((dictionary)
                .OrderBy(kvp => kvp.Key));
    }
}
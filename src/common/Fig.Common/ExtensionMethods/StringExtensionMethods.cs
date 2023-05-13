using Newtonsoft.Json;

namespace Fig.Common.ExtensionMethods;

public static class StringExtensionMethods
{
    public static bool TryParseJson<T>(this string data, TypeNameHandling typeNameHandling, out T result)
    {
        var success = true;
        var settings = new JsonSerializerSettings
        {
            Error = (_, args) =>
            {
                success = false; 
                args.ErrorContext.Handled = true;
            },
            MissingMemberHandling = MissingMemberHandling.Error,
            TypeNameHandling = typeNameHandling
        };
        result = JsonConvert.DeserializeObject<T>(data, settings);
        return success;
    }
}
using Newtonsoft.Json;

namespace Fig.Api.ExtensionMethods;

public static class StringExtensionMethods
{
    public static bool TryParseJson<T>(this string data, out T result)
    {
        var success = true;
        var settings = new JsonSerializerSettings
        {
            Error = (_, args) => { success = false; args.ErrorContext.Handled = true; },
            MissingMemberHandling = MissingMemberHandling.Error
        };
        result = JsonConvert.DeserializeObject<T>(data, settings);
        return success;
    }
}
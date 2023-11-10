using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Text;

namespace Fig.Integration.SqlLookupTableService.Test;

public static class ExtensionMethods
{
    public static void AddObject(this IConfigurationBuilder builder, object obj)
    {
        // Serialize the object to a JSON string and add it to the configuration
        var json = JsonConvert.SerializeObject(obj);
        builder.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json)));
    }
}
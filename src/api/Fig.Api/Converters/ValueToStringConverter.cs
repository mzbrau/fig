using System.Globalization;
using Newtonsoft.Json;

namespace Fig.Api.Converters;

public class ValueToStringConverter : IValueToStringConverter
{
    public string Convert(dynamic? value)
    {
        if (value == null)
            return "<NOT SET>";
        
        return value switch
        {
            char c => c.ToString(),
            short s => s.ToString(CultureInfo.InvariantCulture),
            int i => i.ToString(CultureInfo.InvariantCulture),
            bool b => b.ToString(),
            long l => l.ToString(CultureInfo.InvariantCulture),
            double d => d.ToString(CultureInfo.InvariantCulture),
            DateTime dt => dt.ToString("u"),
            TimeSpan ts => ts.ToString(),
            string s => s,
            DateOnly d => d.ToShortDateString(),
            TimeOnly t => t.ToShortTimeString(),
            _ => JsonConvert.SerializeObject(value)
        };
    }
}
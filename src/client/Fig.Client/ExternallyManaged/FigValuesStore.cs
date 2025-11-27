using System.Collections.Generic;

namespace Fig.Client.ExternallyManaged;

public static class FigValuesStore
{
    private static readonly object LockObject = new();
    private static Dictionary<string, string?> _figValues = new();

    public static void StoreFigValues(Dictionary<string, string?> values)
    {
        lock (LockObject)
        {
            foreach (var kvp in values)
            {
                _figValues[kvp.Key] = kvp.Value;
            }
        }
    }

    public static Dictionary<string, string?> GetFigValues()
    {
        lock (LockObject)
        {
            return new Dictionary<string, string?>(_figValues);
        }
    }

    public static void Clear()
    {
        lock (LockObject)
        {
            _figValues.Clear();
        }
    }
}

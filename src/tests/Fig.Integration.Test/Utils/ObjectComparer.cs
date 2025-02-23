using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace Fig.Integration.Test.Utils;

public static class ObjectComparer
{
    public static bool AreEquivalent(object? expected, object? actual, out string message, params List<string> excludedProperties)
    {
        if (expected == null && actual == null)
        {
            message = "Both were null";
            return true;
        }

        if (expected == null || actual == null)
        {
            message = "one was null";
            return false;
        }

        var expectedType = expected.GetType();
        var actualType = actual.GetType();

        var expectedProperties = expectedType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var actualProperties = actualType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in expectedProperties)
        {
            if (excludedProperties.Contains(property.Name))
            {
                continue;
            }

            var actualProperty = actualProperties.FirstOrDefault(p => p.Name == property.Name);
            if (actualProperty == null)
            {
                continue;
            }

            var expectedValue = property.GetValue(expected);
            var actualValue = actualProperty.GetValue(actual);

            if (expectedValue == null && actualValue == null)
            {
                continue;
            }

            if (expectedValue == null || actualValue == null)
            {
                message = $"One value of {property.Name} was null. Expected:{expectedValue}, Actual:{actualValue}";
                return false;
            }

            var expectedJson = JsonConvert.SerializeObject(expectedValue);
            var actualJson = JsonConvert.SerializeObject(actualValue);
            
            if (!expectedJson.Equals(actualJson))
            {
                message = $"Values of {property.Name} where different. Expected:{expectedValue}, Actual:{actualValue}";
                return false;
            }
        }

        message = "Where equivalent";
        return true;
    }
}
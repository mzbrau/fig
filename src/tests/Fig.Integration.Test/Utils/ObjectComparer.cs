using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Fig.Integration.Test.Utils;

public static class ObjectComparer
{
    public static bool AreEquivalent(object? expected, object? actual, params List<string> excludedProperties)
    {
        if (expected == null && actual == null)
        {
            return true;
        }

        if (expected == null || actual == null)
        {
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
                return false;
            }

            if (!expectedValue.Equals(actualValue))
            {
                return false;
            }
        }

        return true;
    }
}
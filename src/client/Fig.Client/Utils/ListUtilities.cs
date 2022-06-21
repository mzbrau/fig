using System;
using System.Collections.Generic;
using System.Linq;

namespace Fig.Client.Utils;

public static class ListUtilities
{
    public static bool TryGetGenericListType(Type type, out Type innerType)
    {
        var interfaceTest = new Func<Type, Type>(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>)
                ? i.GetGenericArguments().Single()
                : null);

        innerType = interfaceTest(type);
        if (innerType != null)
        {
            return true;
        }

        foreach (var i in type.GetInterfaces())
        {
            innerType = interfaceTest(i);
            if (innerType != null)
            {
                return true;
            }
        }

        return false;
    }
}
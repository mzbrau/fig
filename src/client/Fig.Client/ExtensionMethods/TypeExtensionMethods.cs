using System;
using System.Collections.Generic;
using System.Linq;
using Fig.Contracts;

namespace Fig.Client.ExtensionMethods
{
    public static class TypeExtensionMethods
    {
        public static bool IsFigSupported(this Type type)
        {
            if (SupportedTypes.All.Contains(type.FullName))
                return true;

            if (type.BaseType == typeof(Enum))
                return true;

            if (type.IsArray)
                return SupportedTypes.All.Contains(type.GetElementType().FullName);

            if (IsCollection(type))
            {
                var arguments = type.GenericTypeArguments;
                if (arguments.Count() == 1)
                    return SupportedTypes.All.Contains(arguments[0].FullName);

                // In the future these might call recursively - need web client support
                return SupportedTypes.All.Contains(arguments[0].FullName) &&
                       SupportedTypes.All.Contains(arguments[1].FullName);
            }

            return false;
        }

        public static bool IsEnum(this Type type)
        {
            return type.BaseType == typeof(Enum);
        }

        private static bool IsCollection(Type type)
        {
            if (!type.IsGenericType)
                return false;

            var typeDefinition = type.GetGenericTypeDefinition();
            return typeDefinition == typeof(Dictionary<,>) ||
                   typeDefinition == typeof(List<>) ||
                   typeDefinition == typeof(KeyValuePair<,>);
        }
    }
}
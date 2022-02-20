using System;
using System.Collections.Generic;
using Fig.Contracts;

namespace Fig.Client.ExtensionMethods
{
    public static class TypeExtensionMethods
    {
        public static bool IsSupportedBaseType(this Type type)
        {
            return SupportedTypes.All.Contains(type.FullName) ||
                   IsEnum(type);
        }

        public static bool IsSupportedDataGridType(this Type type)
        {
            if (!IsGenericList(type))
                return false;

            var arguments = type.GenericTypeArguments;
            if (arguments.Length == 1)
                return SupportedTypes.All.Contains(arguments[0].FullName) ||
                       arguments[0].IsClass;

            return false;
        }

        public static bool IsEnum(this Type type)
        {
            return type.BaseType == typeof(Enum);
        }

        private static bool IsGenericList(Type type)
        {
            if (!type.IsGenericType)
                return false;

            var typeDefinition = type.GetGenericTypeDefinition();
            return typeDefinition == typeof(List<>);
        }
    }
}
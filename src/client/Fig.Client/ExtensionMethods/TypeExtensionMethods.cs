using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fig.Client.ExtensionMethods
{
    public static class TypeExtensionMethods
    {
        private static List<string> _supportedTypes = new List<string>()
        {
            "System.Boolean",
            //"System.Byte",
            "System.Char",
            "System.Double",
            "System.Int16",
            "System.Int32",
            "System.Int64",
            //"System.IntPtr",
            //"System.SByte",
            "System.Single",
            //"System.UInt16",
            //"System.UInt32",
            //"System.UInt64",
            //"System.UIntPtr",
            "System.String"
        };

        public static bool IsFigSupported(this Type type)
        {
            if (_supportedTypes.Contains(type.FullName))
                return true;

            if (type.BaseType == typeof(Enum))
                return true;

            if (type.IsArray)
                return _supportedTypes.Contains(type.GetElementType().FullName);

            if (IsCollection(type))
            {
                var arguments = type.GenericTypeArguments;
                if (arguments.Count() == 1)
                {
                    return _supportedTypes.Contains(arguments[0].FullName);
                }

                // In the future these might call recursively - need web client support
                return _supportedTypes.Contains(arguments[0].FullName) &&
                       _supportedTypes.Contains(arguments[1].FullName);
            }

            return false;
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

using System;
using System.Runtime.CompilerServices;

namespace Fig.Contracts.ExtensionMethods
{
    public static class TypeExtensionMethods
    {
        public static bool Is(this Type type, string typeName)
        {
            return type.FullName == typeName;
        }
    }
}
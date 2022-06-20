using System;
using System.Collections.Generic;
using System.Security;
using Fig.Contracts;
using Fig.Contracts.ExtensionMethods;

namespace Fig.Client.ExtensionMethods;

public static class TypeExtensionMethods
{
    public static bool IsSupportedBaseType(this Type type)
    {
        var figType = type.FigPropertyType();
        return figType != FigPropertyType.Unsupported && figType != FigPropertyType.DataGrid ||
               IsEnum(type);
    }

    public static bool IsSupportedDataGridType(this Type type)
    {
        if (!IsGenericList(type))
            return false;

        var arguments = type.GenericTypeArguments;
        if (arguments.Length == 1)
            return arguments[0].FigPropertyType() != FigPropertyType.Unsupported ||
                   arguments[0].IsClass;

        return false;
    }

    public static bool IsEnum(this Type type)
    {
        return type.BaseType == typeof(Enum);
    }

    public static bool IsSecureString(this Type type)
    {
        if (type == typeof(SecureString))
            return true;

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            return Nullable.GetUnderlyingType(type) == typeof(SecureString);

        return false;
    }

    private static bool IsGenericList(Type type)
    {
        if (!type.IsGenericType)
            return false;

        var typeDefinition = type.GetGenericTypeDefinition();
        return typeDefinition == typeof(List<>);
    }
}
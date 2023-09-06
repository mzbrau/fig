using System;
using System.Collections.Generic;
using System.Security;
using System.Text.RegularExpressions;

namespace Fig.Contracts.ExtensionMethods
{
    public static class TypeExtensionMethods
    {
        private static readonly Dictionary<string, FigPropertyType> _propertyTypeMappings =
            new Dictionary<string, FigPropertyType>
            {
                {"System.Boolean", Contracts.FigPropertyType.Bool},
                {"System.Double", Contracts.FigPropertyType.Double},
                {"System.Int32", Contracts.FigPropertyType.Int},
                {"System.Int64", Contracts.FigPropertyType.Long},
                {"System.String", Contracts.FigPropertyType.String},
                {"System.Security.SecureString", Contracts.FigPropertyType.String},
                {"System.DateTime", Contracts.FigPropertyType.DateTime},
                {"System.TimeSpan", Contracts.FigPropertyType.TimeSpan},

                {
                    "System.Collections.Generic.List`1[[System.Collections.Generic.Dictionary`2[[System.String, System.Private.CoreLib,  Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Object, System.Private.CoreLib,  Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib,  Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]",
                    Contracts.FigPropertyType.DataGrid
                },
                {
                    "Newtonsoft.Json.Linq.JArray",
                    Contracts.FigPropertyType.DataGrid
                },
                {
                    "System.Nullable`1[[System.Boolean, System.Private.CoreLib,  Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]",
                    Contracts.FigPropertyType.Bool
                },
                {
                    "System.Nullable`1[[System.Double, System.Private.CoreLib,  Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]",
                    Contracts.FigPropertyType.Double
                },
                {
                    "System.Nullable`1[[System.Int32, System.Private.CoreLib,  Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]",
                    Contracts.FigPropertyType.Int
                },
                {
                    "System.Nullable`1[[System.Int64, System.Private.CoreLib,  Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]",
                    Contracts.FigPropertyType.Long
                },
                {
                    "System.Nullable`1[[System.String, System.Private.CoreLib,  Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]",
                    Contracts.FigPropertyType.String
                },
                {
                    "System.Nullable`1[[System.DateTime, System.Private.CoreLib,  Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]",
                    Contracts.FigPropertyType.DateTime
                },
                {
                    "System.Nullable`1[[System.TimeSpan, System.Private.CoreLib,  Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]",
                    Contracts.FigPropertyType.TimeSpan
                }
            };

        public static FigPropertyType FigPropertyType(this Type? type)
        {
            if (type?.FullName != null)
            {
                var versionLessKey = Regex.Replace(type.FullName, @"Version=\d*\.\d*\.\d*\.\d*,", string.Empty);
                if (_propertyTypeMappings.ContainsKey(versionLessKey))
                    return _propertyTypeMappings[versionLessKey];
            }

            return Contracts.FigPropertyType.Unsupported;
        }

        public static bool Is(this Type type, FigPropertyType propertyType)
        {
            var figType = type.FigPropertyType();
            return figType == propertyType;
        }
        
        public static bool IsSupportedBaseType(this Type type)
        {
            var figType = type.FigPropertyType();
            return figType != Contracts.FigPropertyType.Unsupported && figType != Contracts.FigPropertyType.DataGrid ||
                   IsEnum(type);
        }

        public static bool IsSupportedDataGridType(this Type type)
        {
            if (!IsGenericList(type))
                return false;

            var arguments = type.GenericTypeArguments;
            if (arguments.Length == 1)
                return arguments[0].FigPropertyType() != Contracts.FigPropertyType.Unsupported ||
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
}
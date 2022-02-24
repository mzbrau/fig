using System;
using System.Collections.Generic;

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
                {"System.DateTime", Contracts.FigPropertyType.DateTime},
                {"System.TimeSpan", Contracts.FigPropertyType.TimeSpan},

                {
                    "System.Collections.Generic.List`1[[System.Collections.Generic.Dictionary`2[[System.String, System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Object, System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]",
                    Contracts.FigPropertyType.DataGrid
                },

                {
                    "System.Nullable`1[[System.Boolean, System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]",
                    Contracts.FigPropertyType.Bool
                },
                {
                    "System.Nullable`1[[System.Double, System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]",
                    Contracts.FigPropertyType.Double
                },
                {
                    "System.Nullable`1[[System.Int32, System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]",
                    Contracts.FigPropertyType.Int
                },
                {
                    "System.Nullable`1[[System.Int64, System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]",
                    Contracts.FigPropertyType.Long
                },
                {
                    "System.Nullable`1[[System.String, System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]",
                    Contracts.FigPropertyType.String
                },
                {
                    "System.Nullable`1[[System.DateTime, System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]",
                    Contracts.FigPropertyType.DateTime
                },
                {
                    "System.Nullable`1[[System.TimeSpan, System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]",
                    Contracts.FigPropertyType.TimeSpan
                }
            };

        public static FigPropertyType FigPropertyType(this Type? type)
        {
            if (type?.FullName != null && _propertyTypeMappings.ContainsKey(type.FullName))
                return _propertyTypeMappings[type.FullName];

            return Contracts.FigPropertyType.Unsupported;
        }

        public static bool Is(this Type type, FigPropertyType propertyType)
        {
            var figType = type.FigPropertyType();
            return figType == propertyType;
        }
    }
}
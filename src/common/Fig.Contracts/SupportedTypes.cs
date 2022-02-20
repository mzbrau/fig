using System.Collections.Generic;

namespace Fig.Contracts
{
    public static class SupportedTypes
    {
        public const string Bool = "System.Boolean";
        public const string Double = "System.Double";
        public const string Int = "System.Int32";
        public const string Long = "System.Int64";
        public const string String = "System.String";
        public const string DateTime = "System.DateTime";
        public const string DateOnly = "System.DateOnly";
        public const string TimeOnly = "System.TimeOnly";
        public const string TimeSpan = "System.TimeSpan";

        // Special type
        // TODO: Make sure this stands up for other dotnet versions.
        public const string DataGrid =
            "System.Collections.Generic.List`1[[System.Collections.Generic.Dictionary`2[[System.String, System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Object, System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]";

        //                              System.Collections.Generic.IEnumerable`1[[System.Collections.Generic.Dictionary`2[[System.String, System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Object, System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
        public static List<string> All { get; } = new List<string>
        {
            Bool,
            Double,
            Int,
            Long,
            String,
            DateTime,
            DateOnly,
            TimeOnly,
            TimeSpan
        };
    }
}
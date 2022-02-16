using System.Collections.Generic;

namespace Fig.Contracts
{
    public static class SupportedTypes
    {
        public const string Bool = "System.Boolean";
        public const string Char = "System.Char";
        public const string Double = "System.Double";
        public const string Short = "System.Int16";
        public const string Int = "System.Int32";
        public const string Long = "System.Int64";
        public const string Decimal = "System.Decimal";
        public const string Single = "System.Single";
        public const string String = "System.String";
        public const string DateTime = "System.DateTime";
        public const string DateOnly = "System.DateOnly";
        public const string TimeOnly = "System.TimeOnly";
        public const string TimeSpan = "System.TimeSpan";
        
        public static List<string> All { get; } = new List<string>()
        {
            Bool,
            Char,
            Double,
            Short,
            Int,
            Long,
            Decimal,
            Single,
            String,
            DateTime,
            DateOnly,
            TimeOnly,
            TimeSpan
        };
    }
}